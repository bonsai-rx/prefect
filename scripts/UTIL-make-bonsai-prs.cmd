:: Submits all PRs from NgrDavid to the target organization
@setlocal
@call config.cmd
@echo Are you sure?
@pause
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/arduino
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/aruco
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/daqmx
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/ffmpeg
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/gui
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/harp
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/ironpython-scripting
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/machinelearning --draft
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/mixer
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/movenet --draft
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/numerics
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/pulsepal
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/pylon
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/python-scripting
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/sgen
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/sleap
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/spinnaker
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/tld --draft
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/vimba --draft
::gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base main --repo %DEPLOY_ORG%/zeromq
gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base master --repo %DEPLOY_ORG%/deeplabcut
gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base master --repo %DEPLOY_ORG%/ephys
gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base master --repo %DEPLOY_ORG%/pointgrey
gh pr create --title "Common CI/CD and repository layout" --body-file BonsaiPrBody.md --reviewer glopesdev --head NgrDavid:main --base master --repo %DEPLOY_ORG%/video
