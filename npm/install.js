#!/usr/bin/env node

"use strict";

const path = require('path'),
      fs = require('fs'),
      axios = require('axios'),
      AdmZip = require("adm-zip"),
      tmp = require('tmp'),
      node_modules = require('node_modules-path');

// Mapping from Node's `process.arch` to dotnet's `RID`
const ARCH_MAPPING = {
    "ia32": "x86",
    "x64": "x64",
};
// Mapping between Node's `process.platform` to dotnet's RID
const PLATFORM_MAPPING = {
    "darwin": "osx",
    "linux": "linux",
    "win32": "win"
};


const installScriptLocation = path.resolve(node_modules(process.env.npm_package_name), process.env.npm_package_name, 'bin');

// delete empty cmf-sdk;
// this file must exist during npm install so the bin link is created;
// it will be replaced by the downloaded binary
fs.rm(path.join(installScriptLocation, 'cmf-sdk'), { force: true, recursive: true }, (err) => {
    if (err) {
        // File deletion failed
        console.error(err.message);
        process.exit(1);
    }
});

// download respective release zip from github
console.info(`Current platform / arch: ${process.platform} / ${process.arch}`);
const pkgUrl = `https://github.com/criticalmanufacturing/portal-sdk/releases/download/${process.env.npm_package_version}/Cmf.CustomerPortal.Sdk.Console-${process.env.npm_package_version}.${PLATFORM_MAPPING[process.platform]}-${ARCH_MAPPING[process.arch]}.zip`;// opts.binUrl.replace("{{version}}", opts.version).replace("{{platform}}", PLATFORM_MAPPING[process.platform]).replace("{{arch}}", ARCH_MAPPING[process.arch]);
console.info(`Getting release archive from ${pkgUrl} into ${path.resolve(installScriptLocation)}`);
axios.get(pkgUrl, { responseType: 'arraybuffer' })
     .then(function (response) {
         // handle success
         const zip = tmp.tmpNameSync();
         console.log(`Writing temporary zip file to ${zip}`);
         fs.writeFileSync(zip, response.data);
         console.log(`Extracting zip file ${zip} to ${installScriptLocation}`);
         (new AdmZip(zip)).extractAllTo(installScriptLocation);
     })
     .catch(function (error) {
         // handle error
         console.error(error);
         console.error(error(`Could not install version ${process.env.npm_package_version} on your platform ${process.platform}/${process.arch}: ${e.message}`));
         process.exit(1);
     });
