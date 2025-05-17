:: Prints the status of the most recent CI run in each Harp repo
@setlocal
@call config.cmd
@echo off
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/arduino
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/aruco
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/daqmx
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/deeplabcut
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/ephys
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/ffmpeg
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/gui
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/harp
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/ironpython-scripting
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/machinelearning
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/mixer
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/movenet
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/numerics
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/pointgrey
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/pulsepal
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/pylon
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/python-scripting
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/sgen
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/sleap
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/spinnaker
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/tld
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/video
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/vimba
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/zeromq
