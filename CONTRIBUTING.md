# Setup for collaborators

Two one-time setup steps are needed on every machine that works on this repo.
Neither can be stored inside the repo itself (git intentionally keeps
executable paths out of versioned config for security), so each collaborator
runs them once, locally.

## 1. Git LFS

Binary assets (textures, sprites, audio, etc. — see `.gitattributes`) are
stored via Git LFS, not plain git.

```bash
git lfs install
```

If you cloned the repo *before* running this, fetch the real files with:

```bash
git lfs pull
```

## 2. Unity scene/prefab merge tool

`.unity`, `.prefab`, `.asset`, `.mat`, `.anim`, and `.controller` files are
structured YAML, not plain text. A normal git text-merge can produce
conflict markers that look resolvable but actually corrupt object
references. `.gitattributes` already points these file types at a merge
driver named `unityyamlmerge` — but the driver has to be registered locally,
pointing at the copy of `UnityYAMLMerge` that ships inside your installed
Unity Editor.

Run the command for your OS (swap in your actual Unity install path/version
if it differs):

**macOS**
```bash
git config merge.unityyamlmerge.driver "'/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/Helpers/UnityYAMLMerge' merge -p --force %O %A %B %A"
```

**Windows** (Git Bash / PowerShell)
```bash
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.5.4f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p --force %O %A %B %A"
```

**Linux**
```bash
git config merge.unityyamlmerge.driver "'$HOME/Unity/Hub/Editor/6000.5.4f1/Editor/Data/Tools/UnityYAMLMerge' merge -p --force %O %A %B %A"
```

Without this, a conflicting merge/pull on a scene or prefab will fall back to
a plain text merge — resolve those by hand only as a last resort, and
double-check the result opens cleanly in the Editor afterward.

## 3. Unity Editor version

Use the exact version in `ProjectSettings/ProjectVersion.txt`
(currently **6000.5.4f1**) to avoid Editor-upgrade diffs across
`ProjectSettings/`.
