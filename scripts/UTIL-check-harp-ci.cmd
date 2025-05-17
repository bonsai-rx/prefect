:: Prints the status of the most recent CI run in each Harp repo
@setlocal
@call config.cmd
@echo off
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.analoginput
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.audioswitch
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.behavior
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.cameracontroller
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.cameracontrollergen2
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.clocksynchronizer
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.faststepper
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.inputexpander
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.ledarray
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.loadcells
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.olfactometer
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.outputexpander
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.rfidreader
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.rgbarray
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.soundcard
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.stepperdriver
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.synchronizer
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.syringepump
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.timestampgeneratorgen3
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.vestibularH1
gh run list --event push --limit 1 --repo %DEPLOY_ORG%/device.vestibularH2
