# Tune Review CLI — Product Requirements and User Stories

This document specifies requirements for a C# command-line application that helps a musician maintain a large repertoire of already-learned folk tunes stored in an Obsidian vault. The tool selects a manageable number of tunes to review on a regular basis, records review state outside the Obsidian notes, and supports filtering by tune metadata such as regional origin.

The requirements are written as an independent backlog suitable for implementation planning and AI-assisted coding.

---

## 1. Goals and Non-Goals

### 1.1 Goals
- Select a weekly (or ad-hoc) set of tunes to review from an Obsidian vault.
- Operate on tunes that are already learned (exclude tunes still being learned).
- Prevent “I haven’t heard that in years” loss by prioritizing neglected/overdue tunes.
- Store all review state in a single JSON file outside (or hidden within) the vault so that tune notes remain unchanged during normal operation.
- Support filtering by “origin” (regionality) and other front matter properties.
- Support an interactive review session with quick scoring and state updates.
- Support marking tunes as “session-maintained” or “never needs review”.

### 1.2 Non-Goals
- The tool does not render music notation or open files in a UI (optional future enhancement: print paths/links only).
- The tool does not depend on Obsidian plugins (Dataview etc. are optional and out of scope).
- The tool does not require modification of existing tune metadata schemas beyond adding a stable `tuneId` (one-time operation).
- The tool does not implement full Anki/SM-2 scheduling; it uses a repertoire-maintenance schedule.

---

## 2. Data Sources and Storage

### 2.1 Source of Truth: Tune Notes
- Tune notes are Markdown files within an Obsidian vault.
- Default location within the vault for tunes is `<vaultRoot>/Tunes/Tunes/` 
- Tune metadata is stored in YAML front matter.
- The application reads YAML front matter to access tune metadata and the stable tune identifier (`tuneId`).

### 2.2 Review State Storage
- Review state is stored in a single JSON file (the “review store”).
- Default location (configurable): `<vaultRoot>/.tune-review/reviews.json`
- The review store must be safe to place outside the vault as well (e.g., user home directory), via CLI flag.

### 2.3 Tune Identifier (`tuneId`)
- Each tune note must contain a stable unique ID in YAML front matter:
  - `tuneId: "<guid>"`
- This ID must persist across file renames and moves.
- Normal operation must not edit notes. The only feature allowed to edit notes is the one-time ID initialization/backfill command.

---

## 3. Metadata Conventions in Tune Notes

### 3.1 Minimum Required Fields
- `tuneId` (GUID string) — required for linking note ↔ review store.
- `learn` (boolean) — required for filtering learned vs learning tunes.
  - The scheduler includes only tunes with `learn != true`. Tunes with `learn: false` or missing `learn` are treated as learned by default (configurable).

### 3.2 Common Optional Fields Used for Filtering/Display
These may be present and should be read (but not required):
- `origin` (string, often Obsidian wiki-link `[[Ref/Geo/...|Display]]`)
- `type` (string/wiki-link)
- `key` (list of strings/wiki-links)
- `mode` (list of strings/wiki-links)
- `whistle` (list of strings/wiki-links)
- `composer` (string/wiki-link)

### 3.3 Wiki-Link Display Text Extraction
Many fields may use Obsidian link syntax:
- `[[Path|Display]]` or `[[Path]]`
The application must be able to extract a human-friendly display string for matching/filtering and console output.

### 3.4 Sample yaml frontmatter

```yaml
type: "[[Ref/Type/Reel|Reel]]"
key: 
  - "[[Ref/Key/A.|A.]]"
mode: 
  - "[[Ref/Mode/A|A]]"
whistle:
  - "[[Ref/Whistle/Tenor D|Tenor D]]"
origin: "[[Ref/Geo/Scottish|Scottish]]"
composer: "[[Ref/Composer/Ali Levack|Ali Levack]]"
sets:
  - "[[Tunes/Sets/Sheepskin & Beeswax (A) - Ricer (D) - D F of T (A)|Sheepskin & Beeswax (A) - Ricer (D) - D F of T (A)]]"
learn: false
learned: 2025-11-11
consolidate: true
sessions:
collections:
  - "[[Ref/Collection/Ali Levack Tune Book, vol. 1|Ali Levack Tune Book, vol. 1]]"
```

---

## 4. Review Store JSON Schema

### 4.1 Schema Versioning
The JSON file must include a schema version for future upgrades:
- `schemaVersion` (integer)
- `updatedUtc` (ISO timestamp string)

### 4.2 Store Structure
Example structure:
```json
{
  "schemaVersion": 1,
  "updatedUtc": "2025-12-21T12:34:56Z",
  "tunes": {
    "<tuneId-guid>": {
      "exclude": false,
      "maintenance": "self",
      "last": "2025-12-21",
      "sessionLast": "2025-12-14",
      "intervalDays": 300,
      "score": 3,
      "notes": "optional text"
    }
  }
}
````

### 4.3 Review Record Fields

For each tuneId, the record may include:

* `exclude` (bool, default false)
    * If true, the tune never appears in scheduled review unless explicitly included.
* `maintenance` (enum string, default `"self"`)
    * Values: `"self"` | `"session"`
    * `"session"` means the tune is maintained by session playing and should be excluded from scheduled review by default.
* `last` (date string `YYYY-MM-DD`, optional)
    * Last time reviewed via this tool.
* `sessionLast` (date string `YYYY-MM-DD`, optional)
    * Last time marked as played at a session via this tool (optional feature).
* `intervalDays` (integer, optional)
    * Current interval length used to calculate due status. If missing, a default interval applies.
* `score` (integer, optional)
    * Last score (0–3).
* `notes` (string, optional)
    * Optional short free-text comment about rustiness, etc.
* `name` (string)
  * The name of the tune, as taken from the title of the source Tune file
  * Only included as reference to make the file more human read-able and scannable

### 4.4 Defaults When Record Missing

If a tune has no record in the store:

* exclude = false
* maintenance = "self"
* last/sessionLast missing
* intervalDays defaults to configured default (e.g., 365)
* score missing

---

## 5. Scheduling and Selection Rules

### 5.1 Eligibility Rules (Default)

A tune is eligible for scheduled review if:

* The tune note is considered learned (learn != true).
* The review record is not excluded (`exclude != true`).
* The review record is not session-maintained (`maintenance != "session"`).

    * Can be overridden by a CLI flag to include session-maintained tunes.

### 5.2 Effective Last Touch

Compute effective last touch date as:

* `effectiveLast = max(last, sessionLast)` ignoring missing values.
* If both are missing, the tune is “never reviewed”.

### 5.3 Due Calculation

* If effectiveLast is missing: tune is considered due immediately.
* Else:

    * dueDate = effectiveLast + intervalDays
    * overdueDays = today - dueDate (positive means overdue)

### 5.4 Selection Priority (Default)

When selecting N tunes:

1. Eligible overdue tunes first, sorted by most overdue (largest overdueDays).
2. Then eligible never-reviewed tunes.
3. Then top-up with eligible non-due tunes sorted by oldest effectiveLast (most neglected).

### 5.5 Interval Update Rules (Repertoire Maintenance)

After reviewing a tune, user inputs a score:

* 0 = failed / blanked
* 1 = rusty / hard
* 2 = fine
* 3 = good
* 4 = solid

Default interval mapping (configurable):

* score 0 -> 4 days
* score 1 -> 10 days
* score 2 -> 30 days
* score 3 -> 90 days
* score 4 -> 300–365 days

When a score is recorded:

* last = today
* score = value
* intervalDays = mapping(score)

### 5.6 Exclusion and Maintenance Actions (Interactive)

During review, the user must be able to:

* Permanently exclude a tune from scheduled review (`exclude = true`).
* Mark a tune as session-maintained (`maintenance = "session"`).

---

## 6. CLI Commands and Features

### 6.1 Global Requirements

* The application must run as a cross-platform C# CLI (Windows/macOS/Linux).
* It must accept a `--vault` path pointing to the Obsidian vault root. (optional; If blank, this should default to the current directory)
* It must accept a `--store` path for the review JSON file (optional; default under vault).
* It must support `--dry-run` for operations that would modify files.
* It must be fast for 1000+ tunes (target: seconds, not minutes).
* It must not require network access.

### 6.2 Command: `ids init`

**Purpose:** One-time initialization to add `tuneId` to notes missing it.

#### Inputs

* `--vault <path>` (required)
* `--root <subfolder>` (optional; default scans all Markdown files; recommended to restrict to Tunes folder)
* `--dry-run` (optional)
* `--limit <n>` (optional)
* `--include-existing` (optional; default false)

#### Behavior

* Scan tune notes for missing `tuneId`.
* Generate a GUID for each missing ID.
* Write the `tuneId` into YAML front matter without altering the note body.
* If a note has no YAML, skip this file and record it as a warning. This should be an edge case.

#### Acceptance Criteria

* In dry-run mode, no files are modified; output lists which files would be updated.
* In write mode, updated notes contain exactly one `tuneId`.
* If duplicates are detected (same tuneId in multiple notes), command reports them and exits non-zero unless `--force` (optional future).

### 6.3 Command: `review`

**Purpose:** Main interactive review session; selects N tunes and updates the review store JSON.

#### Inputs

* `--vault <path>`
* `--count <n>` (required; e.g., 10–15 per week)
* `--origin <text>` (optional filter; exact/contains match on display text)
* `--include-session` (optional; includes maintenance=session tunes)
* `--include-excluded` (optional; includes exclude=true tunes)
* `--default-interval <days>` (optional; default 365)
* `--dry-run` (optional)
* `--print-paths` (optional)
* `--format <plain|json>` (optional; future-friendly; plain default)

#### Behavior

* Load tune notes and parse YAML front matter.
* Filter to learned tunes.
* Load review store JSON (create if missing).
* Merge review state in-memory using tuneId.
* Select N tunes using selection priority.
* Present tunes one-by-one in the terminal:

    * Show title (filename or a parsed title)
    * Show key metadata: origin/type/key/mode/whistle (as available)
    * Show effectiveLast and intervalDays and due/overdue status (as available)
* For each tune, accept an action:

    * `0/1/2/3/4` -> record score and update interval and last
    * `s` -> skip (no changes)
    * `x` -> exclude tune (exclude=true)
    * `m` -> mark as session-maintained (maintenance="session")
    * Optional: `n` -> prompt for a short notes string saved to review record
* After session:

    * Persist updates to review store JSON (unless dry-run)
    * Output a summary: reviewed count, skipped count, updated count, overdue remaining, etc.

#### Acceptance Criteria

* Running `review` never modifies Markdown note content.
* Review store is updated atomically and remains valid JSON even after interruption.
* Tunes returned by selection algorithm are stable and explainable (sorted as specified).
* `--origin` filter works with wiki-link display text and plain strings.

### 6.4 Command: `pick`

**Purpose:** Non-interactive selection; outputs which tunes would be reviewed.

#### Inputs

Same as `review` except no interactive scoring.

#### Behavior

* Applies the same selection algorithm.
* Outputs a list of selected tunes (title + path + key metadata).
* Does not modify the store or notes.

#### Acceptance Criteria

* Output order matches scheduling priority rules.
* Useful for planning practice without committing updates.

### 6.5 Command: `session`

**Purpose:** Mark tunes as played at a session so they don’t need extra review.

#### Inputs

* `--vault <path>`
* `--date YYYY-MM-DD` (optional; default today)
* Selection inputs (at least one required):

    * `--origin <text>` and `--count <n>` (bulk mark)
    * `--from-file <path>` (list of tuneIds or filenames)
    * Optional: `--query <text>` (future)
* `--dry-run` (optional)

#### Behavior

* Resolve selected tunes by tuneId.
* Update `sessionLast` to the given date for each.
* Does not change `maintenance` unless explicitly requested by a flag (optional).

#### Acceptance Criteria

* Session marking updates only the review store.
* EffectiveLast uses sessionLast when calculating due.

### 6.6 Command: `stats`

**Purpose:** Report coverage, overdue counts, and health metrics.

#### Inputs

* `--vault <path>`
* Optional filters: `--origin`, `--type`, etc. (future)

#### Behavior

Report at minimum:

* total learned tunes
* total eligible (after exclude/session-maintained removed)
* count excluded
* count session-maintained
* count never-reviewed
* count overdue
* top 10 most overdue / most neglected (with titles)

#### Acceptance Criteria

* Output helps tune default intervals and weekly target.

---

## 7. Parsing and File Handling Requirements

### 7.1 Markdown Scanning

* Must scan for `*.md` files under vault (optionally restricted by a root folder).
* Must ignore `.obsidian/` and optionally other dot-folders by default.
* Must provide a way to default to `Tunes/Tunes/` (recommended).

### 7.2 YAML Front Matter Parsing

* Must correctly identify YAML front matter delimited by `---` at the start of the file.
* Must parse YAML into a model that can handle:

    * strings
    * booleans
    * lists
* Must tolerate unknown keys and preserve them if writing (only for `ids init` command).

### 7.3 Writing Notes (Only `ids init`)

* Must write minimally:

    * Insert `id` while preserving other YAML keys.
    * Avoid touching note body content.
* Must not reorder YAML keys unless unavoidable (implementation choice); if reordering occurs, document it.

### 7.4 Review Store Persistence

* Must use atomic write:

    * write temp file, fsync/close, rename
* Must optionally keep a `.bak` copy of the last known-good file.
* Must validate JSON before replacing the existing store.

---

## 8. Matching and Display Rules

### 8.1 Title Derivation

* Default title: filename without extension.
* Optional enhancement: if YAML contains a `title` field, use it.

### 8.2 Wiki-Link Display Text Extraction

Given:

* `[[Ref/Geo/Scottish|Scottish]]` -> "Scottish"
* `[[Ref/Geo/Scottish]]` -> "Scottish" (last path segment)
* `Scottish` -> "Scottish"

This should apply to fields used for filters such as `origin`, `type`, `whistle`, etc.

### 8.3 Origin Filtering

* `--origin <text>` must match against display string.
* Matching behavior:

    * default: case-insensitive contains match
    * optional future: exact match flag

---

## 9. Configuration and Defaults

### 9.1 Defaults

* Default review store path: `<vault>/.tune-review/reviews.json`
* Default learned rule: include if `learn != true`
* Default intervalDays for tunes with no record: 365
* Default score→interval mapping: 0→14, 1→60, 2→120, 3→300

### 9.2 Config File (Optional Future Story)

* Support a config file (e.g., `tunes.json`) to store defaults:

    * vault path
    * store path
    * default interval
    * score mapping
    * ignored folders

---

## 10. Backlog: Epics and User Stories

### Epic A — Vault Scanning and Metadata Reading

1. **Story A1:** As a user, I can point the tool at my vault and it finds all tune Markdown files.
2. **Story A2:** As a user, the tool ignores `.obsidian/` and hidden/system folders by default.
3. **Story A3:** As a user, the tool parses YAML front matter into a structured model.
4. **Story A4:** As a user, the tool can extract display text from Obsidian wiki-links for matching and printing.

### Epic B — Review Store (JSON) Management

5. **Story B1:** As a user, the tool creates a review store JSON file if it doesn’t exist.
6. **Story B2:** As a user, the tool loads and merges review state by tuneId.
7. **Story B3:** As a user, store writes are atomic and never corrupt the JSON file.
8. **Story B4:** As a user, the store includes a schemaVersion and can be upgraded later.

### Epic C — Tune ID Initialization

9. **Story C1:** As a user, I can run `ids init` to add missing `tuneId` values to my tune notes.
10. **Story C2:** As a user, `ids init --dry-run` shows what would change without modifying files.
11. **Story C3:** As a user, the tool detects duplicate tuneIds and warns me.

### Epic D — Scheduling and Selection

12. **Story D1:** As a user, I can select N tunes using overdue-first + neglected top-up logic.
13. **Story D2:** As a user, tunes still being learned (`learn: true`) are excluded from selection.
14. **Story D3:** As a user, tunes marked excluded in the store do not appear by default.
15. **Story D4:** As a user, tunes marked session-maintained do not appear by default.
16. **Story D5:** As a user, due status is computed using effectiveLast = max(last, sessionLast).

### Epic E — Interactive Review Session

17. **Story E1:** As a user, I can run `review --count N` and get an interactive list of tunes.
18. **Story E2:** As a user, I can score each tune 0–3 and the tool updates last and intervalDays.
19. **Story E3:** As a user, I can skip a tune without updating anything.
20. **Story E4:** As a user, I can mark a tune as excluded during review.
21. **Story E5:** As a user, I can mark a tune as session-maintained during review.
22. **Story E6 (Optional):** As a user, I can attach a short note to the review record (rustiness, reminders).
23. **Story E7:** As a user, the tool prints a summary of what was reviewed and what remains overdue.

### Epic F — Filtering (Regionality / Origin)

24. **Story F1:** As a user, I can filter review selection to a specific origin (e.g., Northumberland).
25. **Story F2:** As a user, origin matching works whether origin is a wiki-link or plain string.

### Epic G — Non-Interactive Outputs

26. **Story G1:** As a user, I can run `pick` to see today’s/this week’s selected tunes without updating anything.
27. **Story G2:** As a user, I can run `stats` to see overdue/neglected counts and tune health metrics.

### Epic H — Session Marking (Optional but Recommended)

28. **Story H1:** As a user, I can mark a set of tunes as “played at session” on a given date.
29. **Story H2:** As a user, sessionLast affects due calculations without changing note files.

### Epic I — Operational Quality

30. **Story I1:** As a user, commands return non-zero exit codes on failure with helpful error messages.
31. **Story I2:** As a user, the tool handles malformed YAML or missing fields gracefully (skipping files with warnings).
32. **Story I3:** As a user, the tool supports Windows/macOS/Linux paths and UTF-8 filenames.
33. **Story I4:** As a user, I can run with `--dry-run` to guarantee no modifications.

---

## 11. Acceptance Tests (High-Level)

* **AT1:** Running `review` never modifies any Markdown tune note files.
* **AT2:** Running `ids init` adds `tuneId` exactly once to each eligible tune note missing it.
* **AT3:** A tune with `learn: true` never appears in scheduled selection.
* **AT4:** A tune with store record `exclude: true` never appears unless `--include-excluded`.
* **AT5:** A tune with `maintenance: "session"` never appears unless `--include-session`.
* **AT6:** Tunes with the oldest effectiveLast (and/or most overdue) appear first.
* **AT7:** After scoring a tune, the store updates last and intervalDays as per mapping.
* **AT8:** If the application is interrupted during store write, the JSON remains valid and recoverable (via atomic replace and/or backup).

---

## 12. Future Enhancements (Optional)

* Config file for defaults and mappings.
* Additional filters: `--type`, `--key`, `--mode`, `--whistle`, `--composer`.
* Biasing instead of strict filtering for origin (e.g., prefer Northumberland but top-up from elsewhere).
* Output formats: JSON/CSV for downstream reporting.
* “Open in Obsidian” support via `obsidian://open` URIs (platform-dependent).
* Git integration helpers (optional; detect dirty store file, etc.).

---

## i. Basic solution architecture

The .net8 solution should be kept fairly minimal:

- CloudAwesome.FolkTune (class library) 
  - Should contain the logic, models and definitions. 
  - The library should provide a narrow public API.
  - The API will initially be consumed by the CLI project, but could be extended in the future
- CloudAwesome.FolkTune.Reviewer (CLI)
  - This is the initial entry point for the user
  - Includes no/minimal logic or business rules, but consumes the public API from the CloudAwesome.FolkTune library
- CloudAwesome.FolkTunes.Tests
  - All logic and business rules, at a minimum, should be covered by NUnit tests