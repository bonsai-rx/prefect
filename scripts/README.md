Loosely tested, most not useful. Try to understand before running

You'll want to install [the GitHub CLI](https://github.com/cli/cli) and get authenticated with it before using any of these.

Main scripts of itnerest:

* `000-default-main.cmd` Switches the few remaining `master`-default repos to `main`
* `001-enable-github-pages.cmd` Bulk converts repos to use GitHub Actions for Pages deployment
* `UTIL-make-next-harp-release.cmd` - This script will help you bulk release every Harp interface with one command. I got the version numbers from the existing ones in the `csproj`s (and then bumped them)
* `UTIL-check-(bonsai/harp)-(ci/cd).cmd` - Does what it says on the tin, it prints the starts of the most recent `push` or `release` event

Make sure to update `config.cmd` to point to the appropriate org.
