(This PR was created semi-automatically.)

This PR brings the first iteration of the new standardized repository layout and CI workflows.

Major highlights include:
* [bonsai-rx/setup-bonsai](https://github.com/bonsai-rx/setup-bonsai)
* [bonsai-rx/configure-build](https://github.com/bonsai-rx/configure-build) (RIP Python)
* Consistent build and docfx setups
* Everything builds and tests all the time, no more finding out the docs website was broken for 7 years
* The GitHub Actions workflow view has a fun shape
* Packages are published on GitHub continuously
* Packages are published to NuGet.org upon release
* Documentation website can be published continuously or upon release (latter is default, `CONTINUOUS_DOCUMENTATION` variable toggles it.)
* Tooling to make these changes feasible to do and hopefully make it easier keep everything aligned in the future

Please note that there's still much I plan to do. More important than anything is that everything is consistent now which will make it easier to improve things going forward. Please resist the urge to tweak things that aren't outright broken. It's important that these PRs are merged sooner rather than later for anything being actively developed.

If this PR is marked as a draft, it's because the pipeline is known to be broken for some reason. See [my org profile readme](https://github.com/NgrDavid) for links a status overview and links to various assets, along with a list of known-broken and excluded projects along with reasoning.
