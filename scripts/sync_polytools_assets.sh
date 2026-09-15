#!/usr/bin/env bash
# Copies every single Asset of the shared PolyTools World into the Godot
# client, validated, and swaps it in as a whole. Called by PolyTools's own
# sync_game04_consumers.sh; runnable by hand as well. Decided in
# docs/TASKS.md, SYNC-02.
#
#   POLYTOOLS_WORLD_DIR   the PolyTools World to read (default: world01)
#
# Shaped after world01's scripts/sync_polytools_characters.sh, with three
# differences: only singles are taken (sets and palettes are not synced, and a
# single that references one is refused), the target is one directory inside
# the Godot project rather than one per type, and game04's own design keys come
# from design/asset_keys.json. design/ is only ever read.
set -euo pipefail

project_root="$(cd "$(dirname "$0")/.." && pwd)"
source_world_dir="${POLYTOOLS_WORLD_DIR:-$project_root/../PolyTools/worlds/world01}"
source_catalog="$source_world_dir/catalog.json"
design_dir="$project_root/design"
assets_dir="$project_root/src/Cardgame.Client/assets"
target_dir="$assets_dir/polytools"

success_color=''
success_reset=''
error_color=''
error_reset=''
if [[ -z "${NO_COLOR:-}" && -t 1 ]]; then
  success_color=$'\033[1;32m'
  success_reset=$'\033[0m'
fi
if [[ -z "${NO_COLOR:-}" && -t 2 ]]; then
  error_color=$'\033[1;31m'
  error_reset=$'\033[0m'
fi

error_message() {
  printf '%bERROR: %s%b\n' "$error_color" "$1" "$error_reset" >&2
}

finish() {
  local status=$?
  trap - EXIT
  if [[ -n "${staging_dir:-}" ]]; then
    rm -rf -- "$staging_dir" || true
  fi
  if [[ -n "${backup_dir:-}" ]]; then
    rm -rf -- "$backup_dir" || true
  fi
  if ((status == 0)); then
    printf '%b%s%b\n' "$success_color" 'POLYTOOLS SYNC SUCCESS' "$success_reset"
  else
    printf '%b%s%b\n' "$error_color" 'POLYTOOLS SYNC FAILED' "$error_reset" >&2
  fi
  exit "$status"
}
trap finish EXIT

if ! command -v jq >/dev/null 2>&1; then
  error_message 'jq is required to validate PolyTools exports.'
  exit 1
fi

if [[ ! -f "$source_catalog" ]]; then
  error_message "PolyTools catalog not found: $source_catalog"
  exit 1
fi

if ! jq -e '
  .schema_version == 3
  and (.world_key | type == "string")
  and (.world_key | length > 0)
  and (.assets | type == "array")
  and (.assets | length > 0)
  and all(.assets[];
    (.asset_key | type == "string")
    and (.asset_key | length > 0)
    and (.asset_type | type == "string")
    and (.asset_type | length > 0)
    and (.runtime_package | type == "string")
    and (.runtime_package | length > 0)
    and (.asset_type | . == "character" or . == "props" or . == "weapons" or . == "terrain" or . == "items" or . == "icon" or . == "symbols")
    and (.asset_category | . == "single" or . == "set" or . == "palette")
    and (.asset_id | type == "string" and length > 0)
    and (.previous_keys | type == "array" and all(.[]; type == "string" and length > 0))
  )
  and (([.assets[].asset_id] | unique | length) == (.assets | length))
  and (.retired_assets | type == "array" and all(.[];
    (.asset_id | type == "string" and length > 0)
    and (.last_asset_key | type == "string" and length > 0)))
  and (([.assets[].asset_id] - [.retired_assets[].asset_id]) == [.assets[].asset_id])
  and (([.assets[].asset_key] | unique | length) == ([.assets[].asset_key] | length))
' "$source_catalog" >/dev/null; then
  error_message "invalid PolyTools world catalog: $source_catalog"
  exit 1
fi

if ! jq -e '
  .schema_version == 1
  and (.asset_keys | type == "array")
  and all(.asset_keys[]; type == "string" and length > 0)
' "$design_dir/asset_keys.json" >/dev/null 2>&1; then
  error_message "invalid game04 design key list: $design_dir/asset_keys.json"
  exit 1
fi

# The Keys game04 writes down on purpose (SYNC-02: design/ holds what game04
# decides, including which asset a thing uses). A rename in PolyTools ages
# these Keys, and previous_keys is where the new name is found. A living Key
# always wins over any previous_keys entry.
design_asset_keys() {
  jq -r '.asset_keys[]' "$design_dir/asset_keys.json"
  # From G02 every card in design/cards/*.json names its own asset. That field
  # does not exist yet; once it does, read it here in the same shape, e.g.
  #   for card in "$design_dir"/cards/*.json; do
  #     [[ -e "$card" ]] && jq -r '.<field> // empty' "$card"
  #   done
}

while IFS= read -r design_key; do
  [[ -n "$design_key" ]] || continue
  category="$(jq -r --arg key "$design_key" \
    '[.assets[] | select(.asset_key == $key) | .asset_category] | first // ""' "$source_catalog")"
  if [[ "$category" == "single" ]]; then
    continue
  fi
  if [[ -n "$category" ]]; then
    error_message "game04 design data names '$design_key', a PolyTools $category, which game04 does not sync"
    exit 1
  fi
  renamed_to="$(jq -r --arg key "$design_key" '
    [.assets[] | select(.previous_keys | index($key)) | .asset_key] | first // ""
  ' "$source_catalog")"
  retired_as="$(jq -r --arg key "$design_key" '
    [.retired_assets[] | select(.last_asset_key == $key) | .asset_id] | first // ""
  ' "$source_catalog")"
  if [[ -n "$renamed_to" ]]; then
    error_message "game04 design data names '$design_key', which PolyTools now calls '$renamed_to'"
  elif [[ -n "$retired_as" ]]; then
    error_message "game04 design data names '$design_key', an Asset PolyTools has withdrawn"
  else
    error_message "game04 design data names '$design_key', which the PolyTools catalog does not know"
  fi
  exit 1
done < <(design_asset_keys | LC_ALL=C sort -u)

destination_for_type() {
  case "$1" in
    character) printf '%s\n' 'characters' ;;
    props) printf '%s\n' 'props' ;;
    weapons) printf '%s\n' 'weapons' ;;
    terrain) printf '%s\n' 'terrain' ;;
    items) printf '%s\n' 'items' ;;
    icon) printf '%s\n' 'icons' ;;
    symbols) printf '%s\n' 'symbols' ;;
    *)
      error_message "unsupported PolyTools asset type: $1"
      exit 1
      ;;
  esac
}

# Both temporary directories live beside the target and carry a .gdignore, so
# the Godot editor does not import a half-finished sync. The new tree is built
# one level down, in staging/polytools, so the .gdignore stays behind when it
# is moved into place.
mkdir -p "$assets_dir"
staging_dir="$(mktemp -d "$assets_dir/.polytools-staging.XXXXXX")"
backup_dir="$(mktemp -d "$assets_dir/.polytools-backup.XXXXXX")"
: >"$staging_dir/.gdignore"
: >"$backup_dir/.gdignore"
staged_tree="$staging_dir/polytools"
mkdir -p "$staged_tree"

while IFS=$'\t' read -r asset_type asset_key package_path; do
  [[ -n "$asset_key" ]] || continue
  manifest_path="$source_world_dir/$package_path"
  destination_subdir="$(destination_for_type "$asset_type")"

  if [[ ! -f "$manifest_path" ]]; then
    error_message "missing PolyTools manifest: $manifest_path"
    exit 1
  fi

  # A single may place another Asset inside itself. Only singles are synced,
  # so a reference to a set or a palette would point at nothing here.
  unsynced_reference="$(jq -r --slurpfile catalog "$source_catalog" '
    [.components[] | select(.kind == "asset_reference") | .source_asset_key as $ref
      | $catalog[0].assets[] | select(.asset_key == $ref and .asset_category != "single")
      | "\(.asset_key) (\(.asset_category))"] | first // ""
  ' "$manifest_path")"
  if [[ -n "$unsynced_reference" ]]; then
    error_message "PolyTools single '$asset_key' references $unsynced_reference, which game04 does not sync"
    exit 1
  fi

  if ! jq -e \
    --arg key "$asset_key" \
    --arg type "$asset_type" \
    --slurpfile catalog "$source_catalog" \
    '
      .schema_version == 23
      and .asset_key == $key
      and .asset_type == $type
      and .asset_category == "single"
      and (.asset_id | type == "string" and length > 0)
      and (.asset_id == ($catalog[0].assets[] | select(.asset_key == $key) | .asset_id))
      and (.components | all(.[]; . as $component
        | if $component.kind == "asset_reference" then
          ($component.source_asset_key | type == "string" and length > 0)
          and ($component.source_asset_id | type == "string" and length > 0)
          and any($catalog[0].assets[];
            .asset_key == $component.source_asset_key
            and .asset_id == $component.source_asset_id)
        else ($component | has("source_asset_id") | not) end))
      and (.regions | type == "array")
      and (.regions | all(.[];
        (.region_id | type == "string" and length > 0)
        and (.name | type == "string" and length > 0)
        and (.role | . == "attack" or . == "hurt" or . == "collision" or . == "destructible")
        and (.geometry_source | . == "authored" or . == "component")
        and (.source_component_id | type == "string" and length > 0)
        and (if .geometry_source == "authored" then
          (.vertices | type == "array" and length >= 3)
          and (.indices | type == "array" and length >= 3 and length % 3 == 0)
        else true end)
      ))
      and (.asset_pivot | type == "array" and length == 2 and all(.[]; type == "number" and isfinite))
      and (.components | type == "array" and length > 0)
      and (has("variants") | not)
      and (.attachment_frames | type == "array")
      and (.components | all(.[];
        (.component_id | type == "string" and length > 0)
        and (.name | type == "string" and length > 0)
        and (.local_transform | type == "object")
        and (if .kind == "asset_reference" then
          (has("projection_depth_corners") | not)
        else
          (.projection_depth_corners | type == "array")
          and all(.projection_depth_corners[];
            (.point_id | type == "string" and length > 0)
            and (.position | type == "array" and length == 2 and all(.[]; type == "number" and isfinite))
          )
          and (([.projection_depth_corners[].point_id] | unique | length) == ([.projection_depth_corners[].point_id] | length))
        end)
      ))
    ' "$manifest_path" >/dev/null; then
    error_message "invalid PolyTools $asset_type manifest: $manifest_path"
    exit 1
  fi

  mkdir -p "$staged_tree/$destination_subdir/$asset_key"
  cp "$manifest_path" "$staged_tree/$destination_subdir/$asset_key/manifest.json"
done < <(jq -r '.assets[] | select(.asset_category == "single") | [.asset_type, .asset_key, .runtime_package] | @tsv' "$source_catalog" | sort)

# The catalog game04 keeps lists what it actually received: the singles.
jq '.assets |= map(select(.asset_category == "single"))' "$source_catalog" >"$staged_tree/catalog.json"

if [[ -e "$target_dir" ]]; then
  mv "$target_dir" "$backup_dir/polytools"
fi
if ! mv "$staged_tree" "$target_dir"; then
  if [[ -e "$backup_dir/polytools" ]]; then
    mv "$backup_dir/polytools" "$target_dir"
  fi
  error_message "could not move the new assets into place: $target_dir"
  exit 1
fi
