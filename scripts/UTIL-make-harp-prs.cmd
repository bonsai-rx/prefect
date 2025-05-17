:: Submits all PRs from NgrDavid to the target organization
@setlocal
@call config.cmd
@echo Are you sure?
@pause
@echo Mo you aren't
exit /b
::gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.analoginput
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.analoginput
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.audioswitch
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.behavior
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.cameracontroller
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.cameracontrollergen2
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.clocksynchronizer
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.faststepper
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.inputexpander
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.ledarray
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.loadcells
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.olfactometer
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.outputexpander
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.rfidreader
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.rgbarray
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.soundcard
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.stepperdriver
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.synchronizer
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.syringepump
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.timestampgeneratorgen3
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.vestibularH1
gh pr create --title "Create basic CI/CD for interface package" --body-file HarpPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/device.vestibularH2
