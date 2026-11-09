// Simple Node syntax and integrity test for Electron files
const fs = require('fs');
const path = require('path');

const filesToTest = [
  'package.json',
  'electron-builder.json',
  'src/main/index.js',
  'src/preload/index.js',
  'src/renderer/js/app.js'
];

let allPassed = true;
const rootDir = path.resolve(__dirname, '..');

console.log('Testing Electron Kiosk Diagnostics App files syntax...');

for (const relPath of filesToTest) {
  const fullPath = path.join(rootDir, relPath);
  try {
    if (relPath.endsWith('.json')) {
      const content = fs.readFileSync(fullPath, 'utf8');
      JSON.parse(content);
      console.log(`[PASS] JSON valid: ${relPath}`);
    } else if (relPath.endsWith('.js')) {
      // Test compilation without executing
      const content = fs.readFileSync(fullPath, 'utf8');
      new Function(content); // Checks syntax
      console.log(`[PASS] JavaScript syntax clean: ${relPath}`);
    }
  } catch (err) {
    console.error(`[FAIL] ${relPath}: ${err.message}`);
    allPassed = false;
  }
}

if (!allPassed) {
  process.exit(1);
} else {
  console.log('All Electron application files verified cleanly!');
}
