FROM node:26

# Exercise npm's global install script policy even if the base image bundles npm 11.
RUN npm install --global npm@12.2.3
ENV NPM_CONFIG_STRICT_ALLOW_SCRIPTS=true
