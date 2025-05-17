:: For all repos still using master, migrates them to main
@setlocal
@call config.cmd
gh api repos/%DEPLOY_ORG%/bitalino/branches/master/rename -f new_name=main -q .name || exit /b
gh api repos/%DEPLOY_ORG%/bpod/branches/master/rename -f new_name=main -q .name || exit /b
gh api repos/%DEPLOY_ORG%/cmt/branches/master/rename -f new_name=main -q .name || exit /b
gh api repos/%DEPLOY_ORG%/deeplabcut/branches/master/rename -f new_name=main -q .name || exit /b
gh api repos/%DEPLOY_ORG%/ephys/branches/master/rename -f new_name=main -q .name || exit /b
gh api repos/%DEPLOY_ORG%/kinect/branches/master/rename -f new_name=main -q .name || exit /b
gh api repos/%DEPLOY_ORG%/physics/branches/master/rename -f new_name=main -q .name || exit /b
gh api repos/%DEPLOY_ORG%/pointgrey/branches/master/rename -f new_name=main -q .name || exit /b
gh api repos/%DEPLOY_ORG%/realsense/branches/master/rename -f new_name=main -q .name || exit /b
gh api repos/%DEPLOY_ORG%/ueye/branches/master/rename -f new_name=main -q .name || exit /b
gh api repos/%DEPLOY_ORG%/video/branches/master/rename -f new_name=main -q .name || exit /b
gh api repos/%DEPLOY_ORG%/vr/branches/master/rename -f new_name=main -q .name || exit /b
