# Tune Review CLI — Post-Initial Go live Product Requirements and User Stories

Having implemented an initial MVP version of the CLI and it being in use, we have gathered a number of primary functionality that can greatly improve the user experience.

The following requirements are to be implemented in the next iteration of the CLI.

---

## 1. Always default to the `Tunes/Tunes` folder for scanning

Currently, the CLI scans the entire directory tree for tunes, which takes up scanning time unnecessarily and also returns a few errors when incompatible files are scanned.

The vault can accept a sub-folder option but as the directory structure is mandated, this is perhaps redundant, the correct default should be to the correct followed always.

### 1.1 Mandated Obsidian directory structure

The minimum mandated directory structure is: <br/>
/ <br/>
/.tune-review/ <br/>
/Queries/ <br/>
/Ref/ <br/>
/Templates/ <br/>
/Tunes/ <br/>
/Tunes/Dots/ <br/>
/Tunes/Sets/ <br/>
/Tunes/Tunes/ <br/>

Where:
- `/.tune-review` contains the CLI-managed reviews.json file (Used by CLI, not typically viewed or updated by users) 
- `/Queries` contains user queries (not required for the CLI)
- `/Ref` contains master data definitions, such as composers, geos, keys and types (not required for the CLI) 
- `/Templates` contains user templates for creating new tunes, sets or composers, etc. (not required for the CLI)
- `/Tunes` contains all the tunes, all images of music, all sets
- `/Tunes/Tunes/` contains the actual tune markdown files to be scanned
- `/Tunes/Dots/` contains image files of sheet music, referenced by the tune markdown files

### 1.2 Review and update all scanners to scan only required folders/files

Currently, the CLI scans all files in the vault, including those in `/Queries`, `/Ref`, `/Templates`, `/Tunes/Dots/` and `/Tunes/Sets/` etc.

The CLI should be updated to:
- Get the current directory, assuming it is the root level of the vault
- Validate that the required directory structure is present (principally, access to `/Tunes/Tunes/` and the `/.tune-review/reviews.json`). Fail early if this validation fails.
- Scan only the `/Tunes/Tunes/` directory for tunes (in the `pick` and `review` commands)

This should maintain the current user input behaviour, but reduce the scanning time, and reduce the number of errors that can be returned. No regression in current functionality should be introduced.

### 1.3 Maintain the ability to direct the CLI to scan a specific directory

While the default should be the current directory (in which case, no other input options are required), the existing `VaultPath` option telling the CLI where to look for the vault should be maintained as an override in case the user wants to overide it. 

### 1.4 Maintain future extensibility of the CLI

In the future, the CLI will be extended to support not only reviews of tunes but also reviews of sets.

Therefore, in this enhancement to the scanner behaviour, we should maintain the internal ability to have it scan only the `Tunes/Sets/` directory in the future, even though this will not be exposed to the CLI at this time.

## 2. Deprecate unnecessary flags from the `session` command

The `session` command has a number of flags that are not required and are not used:

```csharp
[CommandOption("--origin <TEXT>")] [Description("Select tunes by origin for bulk marking")]
public string? Origin { get; set; }

[CommandOption("--count <N>")]
[Description("Number of tunes to mark (used with --origin)")]
public int? Count { get; set; }
```

There is no use case when we would want to mark a number of tunes by origin, or limit the number of tunes to be marked.

Both commands should be deprecated and related functionality in the `SessionCommand` class should be removed.

Future functionality will be added to allow a user to input a single tune or delimited list to be marked as a CLI command option, but for now, the MVP `FromFile` option is sufficient. 

## 3. Convert `stats` command into a branch with more options


## 4. Add `admin` branch for initialising and managing the Obsidian branch


## 5. Review, update and extend score intervals for reviews





