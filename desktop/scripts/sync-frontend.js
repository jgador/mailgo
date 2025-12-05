const path = require('path');
const fs = require('fs-extra');

const buildDir = path.resolve(__dirname, '../../frontend/app/build');
const targetDir = path.resolve(__dirname, '../resources/frontend');

async function sync() {
  if (!(await fs.pathExists(buildDir))) {
    throw new Error('Frontend build output not found. Run npm run build in frontend/app first.');
  }

  await fs.remove(targetDir);
  await fs.ensureDir(targetDir);
  await fs.copy(buildDir, targetDir);
  console.log(`Copied frontend build to ${targetDir}`);
}

sync().catch((err) => {
  console.error(err);
  process.exit(1);
});
