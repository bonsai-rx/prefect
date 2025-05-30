Here be dragons and hard-coded paths

Everything here is still in rough prototype state, but these are the main tools I made for migrating all the Bonsai Foundation repos. I hope polish up Prefect and [productize it](https://github.com/bonsai-rx/prefect/issues/4) soonish. (`ForeachRepo` probably isn't especially useful unless you're me.)

* `src/Prefect` - A tool for enforcing a particular set of standards on a set of repos, need to modify `Program.cs` to change parameters. (Good luck lol)
    * `reference` and `reference-harp` are the two reference trees for Bonsai and Harp respectively (not all rules are shown here tough, see `Ruleset.cs`)
* `src/ForeachRepo` - A tool for running commands over a group of repos (with basic filtering and conditionals). EG: Run `ForeachRepo --exclude=machinelearning bonsai-rx git status` to print the status of every repo in the `bonsai-rx` folder (except for the Bonsai.ML repo.) Has some handy built-in commands too.
* `scripts/` - Various scripts for performing bulk actions and querires against Harp/Bonsai repos. Update `config.cmd` to change the target organization (IE: to your fork org.)
