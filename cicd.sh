#!/bin/bash

set -e

CICD_COMMON_VERSION="v2.3"

export CLASS_LIBRARY_PROJ_DIR=Source/Otc.Messaging.Abstractions
# export TEST_PROJ_DIR=Source/Otc.AspNetCore.ApiBoot.Tests

cd $TRAVIS_BUILD_DIR

wget -q https://raw.githubusercontent.com/OleConsignado/otc-cicd-common/$CICD_COMMON_VERSION/cicd-common.sh -O ./cicd-common.sh
chmod +x ./cicd-common.sh

./cicd-common.sh $@

export CLASS_LIBRARY_PROJ_DIR=Source/Otc.Messaging.RabbitMQ
./cicd-common.sh $@

export CLASS_LIBRARY_PROJ_DIR=Source/Otc.Messaging.RabbitMQ.PredefinedTopologies
./cicd-common.sh $@

export CLASS_LIBRARY_PROJ_DIR=Source/Otc.Messaging.Typed.Abstractions
./cicd-common.sh $@

export CLASS_LIBRARY_PROJ_DIR=Source/Otc.Messaging.Typed
./cicd-common.sh $@

export CLASS_LIBRARY_PROJ_DIR=Source/Otc.Messaging.Subscriber.HW
./cicd-common.sh $@