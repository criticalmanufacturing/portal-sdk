#!/usr/bin/env node

"use strict";

const path = require('path'),
      fs = require('fs'),
      axios = require('axios'),
      httpsProxyAgent = require('https-proxy-agent'),
      httpProxyAgent = require('http-proxy-agent'),
      proxyFromEnv = require('proxy-from-env'),
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

// Function to download and extract the zip file
async function downloadAndExtract(pkgUrl) {
    try {
        console.info(`Fetching release archive from ${pkgUrl}`);

        // support for http/s proxies through env vars
        const proxy = proxyFromEnv.getProxyForUrl(pkgUrl);
        let httpAgent, httpsAgent;
        if (proxy) {
            httpAgent = new httpProxyAgent.HttpProxyAgent(proxy);
            httpsAgent = new httpsProxyAgent.HttpsProxyAgent(proxy);
        }

        // make req (override axios automatic proxy since it is not working properly)
        const response = await axios.get(pkgUrl, { httpAgent: httpAgent, httpsAgent: httpsAgent, proxy: false, responseType: 'arraybuffer' });
        
        const zip = tmp.tmpNameSync();
        console.log(`Writing temporary zip file to ${zip}`);
        fs.writeFileSync(zip, response.data);

        console.log(`Extracting zip file ${zip} to ${installScriptLocation}`);
        (new AdmZip(zip)).extractAllTo(installScriptLocation, undefined, true);
        
        rimraf.sync(zip); 
    } catch (error) {
        throw new Error(`Failed to download or extract archive from ${pkgUrl}: ${error.message}`);
    }
}

// Main function to attempt downloading from multiple sources
async function installPackage() {
    console.info(`Current platform / arch: ${process.platform} / ${process.arch}`);

    // Primary URL
    const primaryUrl = new URL(`https://github.com/criticalmanufacturing/portal-sdk/releases/download/#{Version}#/Cmf.CustomerPortal.Sdk.Console-${process.env.npm_package_version}.${PLATFORM_MAPPING[process.platform]}-${ARCH_MAPPING[process.arch]}.zip`);
    
    // Fallback URLs
    const fallbackUrls = [
        new URL(`https://criticalmanufacturing.io/repository/tools/Cmf.CustomerPortal.Sdk.Console-${process.env.npm_package_version}.${PLATFORM_MAPPING[process.platform]}-${ARCH_MAPPING[process.arch]}.zip`),
        new URL(`https://repository.criticalmanufacturing.com.cn/repository/tools/Cmf.CustomerPortal.Sdk.Console-${process.env.npm_package_version}.${PLATFORM_MAPPING[process.platform]}-${ARCH_MAPPING[process.arch]}.zip`)
    ];

    // Try downloading from primary URL and fallbacks
    const allUrls = [primaryUrl, ...fallbackUrls];

    for (const pkgUrl of allUrls) {
        try {
            await downloadAndExtract(pkgUrl.toString());
            console.info(`Package installed successfully from ${pkgUrl.toString()}`);
            return;
        } catch (error) {
            console.error(error.message);
        }
    }

    console.error(`Could not install version ${process.env.npm_package_version} on your platform ${process.platform}/${process.arch}.`);
    process.exit(1);  // Exit the process after all attempts fail
}

installPackage();
       
