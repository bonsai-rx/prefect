# Prefect

Everything here is still in rough prototype state, but these were the main tools made for migrating all the Bonsai Foundation repos.

* `src/Prefect` is a tool for enforcing a particular set of standards on a set of repos.
* `reference` and `reference-harp` are the two reference trees for Bonsai and Harp respectively (not all rules are shown here tough, see `Ruleset.cs`)
* `src/ForeachRepo` is a tool for running commands over a group of repos (with basic filtering and conditionals). EG: Run `ForeachRepo --exclude=machinelearning bonsai-rx git status` to print the status of every repo in the `bonsai-rx` folder, except for the Bonsai.ML repo. Has some handy built-in commands too.
* `scripts/` - Various scripts for performing bulk actions and queries against Harp/Bonsai repos. Update `config.cmd` to change the target organization.

## Usage

```
prefect <reference-template> [repo ...] [--skip repo] [--interactive] [--auto-fix] [--project-name name]
```

### Arguments and flags

#### `<reference-template>`
Required path to the reference template to use for validaiton (see detailed description below.)

#### `[repo ...]`
The repository or set of repositories to validate. Multiple can be specified.

If the specified path is a Git repository, then that single repository will be added to the validation set. If the specified path is a non-Git directory, then each Git repository directly under it is added to the validation set. If no repositories are specified, the list of configured validation rules will be printed.

#### `--skip repo`
Indicates that the specified repository should be skipped, can be specified multiple times. `repo` can be either the name of the repository's folder, the path to a repository, or the repository's project name.

#### `--interactive`
Enables interactive mode. In interactive mode Prefect will pause and wait for the user to correct issues whenever a repository fails validation, after which the repository will be checked again.

#### `--auto-fix`
When enabled, Prefect will automatically fix certain rule violations. (Note that fixes may be destructive.)

#### `--project-name name`
Overrides the project name instead of using automatic detection, cannot be used with sets of multiple repositories. This flag is most useful when provisioning new repositories where the automatic name detection has nothing to work with.

## Reference template

Prefect performs validation against a directory tree used as the reference template.

For each file and directory in the tree, Prefect will validate the file exists in the target and, for files which have contents, Prefect will also validate that the contents match. Reference templates may also contain additional configuration files to control Prefect's behavior, as described in detail below.

### Template configuration files

#### `.prefect-template-kind`
Contains a well-known template kind used to add additional programmatic rules to the rule set.

#### `.prefect-ignore-content`
An index of file paths which are permitted to diverge in the target repo. The contents of the file in the template will be used only if the file does not yet exist in the target.

#### `.prefect-interpolated-files`
An index of file paths in the template which will have their contents interpolated before validation.

#### `.prefect-must-not-exist`
An index of file paths which must not exist in the target, typically used for legacy files which are not longer desired.

### File index format

File indices list one file path per line. Indentation and any trailing whitespace is automatically trimmed. Blank lines and lines beginning with `#` are ignored. Glob syntax is *not* supported.

### Interpolation
The names of all files and the contents of files listed in `.prefect-interpolated-files` will be interpolated using the special `$INTERPOLATION$` syntax.

The following interpolations are supported:
  * `$PROJECT$` - The human-friendly name of a project (typically the package name.)
  * `$REPO-SLUG$` - The name of the repository, which is the name of the folder which contains it.

### Project names
The project name corresponding to a repository is inferred unless specified using `--project-name`. By default, the name of a project will be the name of the shortest-named `.sln` file in the root of the repository. When using a {nameof(TemplateKind.HarpTech)} template, the `Interface` directory is used instead of the root.

## Licensing

`Prefect` is released as open source under the [MIT license](https://licenses.nuget.org/MIT).