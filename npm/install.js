#!/usr/bin/env node

"use strict";

const path = require('path'),
      fs = require('fs'),
      axios = require('axios'),
      AdmZip = require("adm-zip"),
      tmp = require('tmp'),
      rimraf = require('rimraf'),
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

// delete empty cmf-portal;
// this file must exist during npm install so the bin link is created;
// it will be replaced by the downloaded binary
fs.unlink(path.join(installScriptLocation, 'cmf-portal'), (err) => {
    if (err) {
        // File deletion failed
        console.error(err.message);
        process.exit(1);
    }
});

// Function to construct the package URL
const constructUrl = (baseUrl, version) => {
    return `${baseUrl}/releases/download/${version}/Cmf.CustomerPortal.Sdk.Console-${version}.${PLATFORM_MAPPING[process.platform]}-${ARCH_MAPPING[process.arch]}.zip`;
};

// Function to download and extract the zip file
async function downloadAndExtract(pkgUrl, installScriptLocation) {
    try {
        console.info(`Fetching release archive from ${pkgUrl}`);
        const response = await axios.get(pkgUrl, { responseType: 'arraybuffer' });
        
        const zipFile = tmp.tmpNameSync();
        console.log(`Writing temporary zip file to ${zipFile}`);
        fs.writeFileSync(zipFile, response.data);

        console.log(`Extracting zip file ${zipFile} to ${installScriptLocation}`);
        (new AdmZip(zipFile)).extractAllTo(installScriptLocation, true);
        
        rimraf.sync(zipFile);  // Clean up temporary zip file
    } catch (error) {
        throw new Error(`Failed to download or extract archive from ${pkgUrl}: ${error.message}`);
    }
}

// Main function to attempt downloading from multiple sources
async function installPackage() {
    console.info(`Current platform / arch: ${process.platform} / ${process.arch}`);
    const installScriptLocation = path.resolve(__dirname);  // Adjust as necessary

    // Primary URL
    const primaryUrl = `https://github.com/criticalmanufacturing/portal-sdk/releases/download`;
    
    // Fallback URLs
    const fallbackUrls = [
        'https://criticalmanufacturing.io',
        'https://criticalmanufacturing.cn'
    ].map(constructUrl);  // Construct fallback URLs dynamically

    // Try downloading from primary URL and fallbacks
    const allUrls = [primaryUrl, ...fallbackUrls];

    for (const pkgUrl of allUrls) {
        try {
            await downloadAndExtract(pkgUrl, installScriptLocation);
            console.info(`Package installed successfully from ${pkgUrl}`);
            return;
        } catch (error) {
            console.error(error.message);
        }
    }

    console.error('Failed to install package from all available sources.');
    process.exit(1);  // Exit the process after all attempts fail
}

installPackage();
       
