:: Prints the homepage for each repository
@setlocal
@call config.cmd
gh api /repos/%DEPLOY_ORG%/arduino/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/aruco/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/daqmx/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/deeplabcut/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/ephys/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/ffmpeg/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/gui/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/harp/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/ironpython-scripting -q .html_url
gh api /repos/%DEPLOY_ORG%/machinelearning/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/mixer/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/movenet/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/numerics/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/pointgrey/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/pulsepal/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/pylon/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/python-scripting -q .html_url
gh api /repos/%DEPLOY_ORG%/sgen/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/sleap/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/spinnaker/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/tld/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/video/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/vimba/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/zeromq/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/bitalino/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/bpod/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/cleyemulticam/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/cmt/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/kinect/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/physics/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/realsense/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/ueye/pages -q .html_url
gh api /repos/%DEPLOY_ORG%/vr/pages -q .html_url