const _ = require('lodash');
const co = require('co');
const path = require('path');
const fspp = require('node-plop/lib/fs-promise-proxy');

module.exports = co.wrap(function* complexModify(data, cfg, plop) {
  // if not already an absolute path, make an absolute path from the basePath (plopfile location)
  const makeTmplPath = (p) => path.resolve(plop.getPlopfilePath(), p);
  const makeDestPath = (p) => path.resolve(plop.getDestBasePath(), p);

  let { template } = cfg;
  const fileDestPath = makeDestPath(plop.renderString(cfg.path || '', data));

  try {
    if (cfg.templateFile) {
      const templateFile = plop.renderString(cfg.templateFile, data);
      template = yield fspp.readFile(makeTmplPath(templateFile));
    }
    if (template == null) { template = ''; }

    // check path
    const pathExists = yield fspp.fileExists(fileDestPath);

    if (!pathExists) {
      throw new Error('File does not exists');
    } else {
      const fileData = yield fspp.readFile(fileDestPath);

      let location;
      // Split lines
      const lines = fileData.split(/\r?\n/);
      switch (cfg.method) {
        case 'sectionEnd': {
          // Detect specfied section
          const id = lines.findIndex((l) => cfg.section.test(l));
          if (id === -1) {
            throw new Error('Section not found');
          }
          // Detect indent
          const ind = lines.findIndex((l, i) => (l !== '' && i > id && l.match(/^\s*/)[0].length < cfg.indent));
          // Detect section boundary
          const nxt = lines.findIndex((l, i) => (i > id && (i < ind || ind === -1) && cfg.pattern.test(l)));

          if (nxt === -1) {
            if (ind === -1) {
              location = lines.length;
            } else {
              location = ind;
            }
          } else {
            location = nxt;
          }
          if (cfg.prePadding === false) {
            location -= 1;
          }
          break;
        }
        case 'lastOccurance': {
          // Find last occurance
          const lastId = _.findLastIndex(lines, (l) => cfg.pattern.test(l));

          if (lastId === -1) {
            throw new Error('Occurance not found');
          } else {
            location = lastId + 1;
          }
          break;
        }
        case 'by': {
          // Find first occurance
          const firstId = _.findIndex(lines, (l) => cfg.pattern.test(l));

          if (firstId === -1) {
            throw new Error('Occurance not found');
          } else {
            location = firstId;
          }
          break;
        }
        case 'append':
          location = lines.length;
          break;
        default:
          throw new Error('Method not allowed');
      }

      if (cfg.postPadding === false) {
        template = template.trimRight();
      }

      // Check if is block end
      if (cfg.indent >= 2 && location < lines.length) {
        const m = lines[location].match(/^\s*}/);
        if (m && m[0].length === cfg.indent - 1) {
          template = `\n${template.trimRight()}`;
        }
      }

      lines.splice(location, 0, plop.renderString(template, data));

      const resultData = lines.join('\n');
      yield fspp.writeFile(fileDestPath, resultData);
    }

    // return the modified file path (relative to the destination path)
    return fileDestPath.replace(path.resolve(plop.getDestBasePath()), '');
  } catch (err) {
    if (typeof err === 'string') {
      throw err;
    } else {
      throw err.message || JSON.stringify(err);
    }
  }
});
