:: Some of these might already have the PackageType updated!
:: I just grabbed the verisons currently in the csproj and bumped the patch version
@setlocal
@call config.cmd
gh release create --generate-notes api0.2.1 --repo %DEPLOY_ORG%/device.analoginput
gh release create --generate-notes api0.2.1 --repo %DEPLOY_ORG%/device.audioswitch
gh release create --generate-notes api0.2.1 --repo %DEPLOY_ORG%/device.behavior
gh release create --generate-notes api0.1.1 --repo %DEPLOY_ORG%/device.cameracontroller
gh release create --generate-notes api0.1.1 --repo %DEPLOY_ORG%/device.cameracontrollergen2
gh release create --generate-notes api0.2.1 --repo %DEPLOY_ORG%/device.clocksynchronizer
gh release create --generate-notes api0.1.1 --repo %DEPLOY_ORG%/device.faststepper
gh release create --generate-notes api0.1.1 --repo %DEPLOY_ORG%/device.inputexpander
gh release create --generate-notes api0.2.1 --repo %DEPLOY_ORG%/device.ledarray
gh release create --generate-notes api0.2.1 --repo %DEPLOY_ORG%/device.loadcells
gh release create --generate-notes api0.2.1 --repo %DEPLOY_ORG%/device.olfactometer
gh release create --generate-notes api0.3.1 --repo %DEPLOY_ORG%/device.outputexpander
gh release create --generate-notes api0.1.1 --repo %DEPLOY_ORG%/device.rfidreader
gh release create --generate-notes api0.1.1 --repo %DEPLOY_ORG%/device.rgbarray
gh release create --generate-notes api0.3.1 --repo %DEPLOY_ORG%/device.soundcard
gh release create --generate-notes api0.4.1 --repo %DEPLOY_ORG%/device.stepperdriver
gh release create --generate-notes api0.2.1 --repo %DEPLOY_ORG%/device.synchronizer
gh release create --generate-notes api0.3.1 --repo %DEPLOY_ORG%/device.syringepump
gh release create --generate-notes api0.2.1 --repo %DEPLOY_ORG%/device.timestampgeneratorgen3
gh release create --generate-notes api0.1.1 --repo %DEPLOY_ORG%/device.vestibularH1
gh release create --generate-notes api0.1.1 --repo %DEPLOY_ORG%/device.vestibularH2
