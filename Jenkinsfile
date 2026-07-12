// CI: build image with Kaniko -> push to GHCR as master-<date>-<time> (+latest).
// Deploy is intentionally MANUAL: use the deploy-<service> job in Jenkins with the
// tag you want (any older tag = rollback).
pipeline {
  agent {
    kubernetes {
      yaml '''
apiVersion: v1
kind: Pod
spec:
  containers:
    - name: kaniko
      image: gcr.io/kaniko-project/executor:debug
      command: ["sleep"]
      args: ["infinity"]
      resources:
        requests: { cpu: "500m", memory: "1Gi" }
        limits: { memory: "2Gi" }
      volumeMounts:
        - name: docker-config
          mountPath: /kaniko/.docker
  volumes:
    - name: docker-config
      secret:
        secretName: ghcr-push
        items:
          - key: .dockerconfigjson
            path: config.json
'''
    }
  }
  options {
    disableConcurrentBuilds()
    buildDiscarder(logRotator(numToKeepStr: '20'))
  }
  environment {
    IMAGE      = 'ghcr.io/ozcancaparoglu/kafadanat-engine'
    DOCKERFILE = 'src/Web.Api/Dockerfile'
  }
  stages {
    stage('Tag') {
      steps {
        script {
          def ts = sh(script: "TZ=Europe/Istanbul date +%Y%m%d-%H%M", returnStdout: true).trim()
          env.IMAGE_TAG = "${env.BRANCH_NAME ?: 'master'}-${ts}"
          currentBuild.displayName = env.IMAGE_TAG
        }
      }
    }
    stage('Build & Push') {
      steps {
        container('kaniko') {
          withCredentials([usernamePassword(credentialsId: 'github-pat',
                                            usernameVariable: 'GITHUB_USER',
                                            passwordVariable: 'GITHUB_TOKEN')]) {
            sh '''/kaniko/executor \
              --context "$WORKSPACE" \
              --dockerfile "$DOCKERFILE" \
              --destination "$IMAGE:$IMAGE_TAG" \
              --destination "$IMAGE:latest" \
              --build-arg GITHUB_USER="$GITHUB_USER" \
              --build-arg GITHUB_TOKEN="$GITHUB_TOKEN" \
              --snapshot-mode=redo --single-snapshot'''
          }
        }
      }
    }
  }
}
