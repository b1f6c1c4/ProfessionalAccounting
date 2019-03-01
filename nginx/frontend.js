const singleLineEditor = (el) => {
  const renderer = new ace.VirtualRenderer(el);
  el.style.overflow = 'hidden';

  renderer.screenToTextCoordinates = function(x, y) {
    const pos = this.pixelToScreenCoordinates(x, y);
    return this.session.screenToDocumentPosition(
      Math.min(this.session.getScreenLength() - 1, Math.max(pos.row, 0)),
      Math.max(pos.column, 0)
    );
  };

  renderer.$maxLines = 1;

  renderer.setStyle('ace_one-line');
  const editor = new ace.Editor(renderer);
  editor.session.setUndoManager(new ace.UndoManager());

  editor.setShowPrintMargin(false);
  editor.renderer.setShowGutter(false);
  editor.renderer.setHighlightGutterLine(false);
  editor.$mouseHandler.$focusWaitTimout = 0;

  return editor;
};

const editor = ace.edit('editor');
const cmdLine = singleLineEditor(document.getElementById('cmdLine'));
cmdLine.editor = editor;
editor.cmdLine = cmdLine;

const freeze = (f) => {
  editor.setReadOnly(f);
  cmdLine.setReadOnly(f);
  document.getElementById('create').disabled = f;
  document.getElementById('upsert').disabled = f;
  document.getElementById('remove').disabled = f;
};

const finalize = (answer, success, insert) => {
  if (!insert) {
    editor.setValue('');
  }
  editor.insert(answer);
  freeze(false);
  if (!success) {
    console.error(err);
  }
};

const prepareObject = () => {
  let row = editor.selection.getCursor().row;
  while (!editor.session.doc.getLine(row).match(/^@new [A-Z][a-z]+ \{/)) {
    if (row) {
      row--;
    } else {
      return undefined;
    }
  }
  const st = row;
  while (!editor.session.doc.getLine(row).match(/\}@$/)) {
    if (row < editor.session.doc.getLength()) {
      row++;
    } else {
      return undefined;
    }
  }
  const ed = row;
  const obj = editor.session.doc.getLines(st, ed).join('\n');
  const rng = new ace.Range(st, 0, ed + 1, 0);
  return { rng, obj };
};

const indicateResult = (res, rng) => {
  freeze(false);
  editor.session.doc.replace(rng, res.endsWith('\n') ? res : res + '\n');
};

const indicateError = (err, { end }) => {
  freeze(false);
  console.error(err);
  editor.session.doc.insert(end, err.endsWith('\n') ? err : err + '\n');
};

const doUpsert = () => {
    ({ rng, obj } = prepareObject());
    freeze(true);
    upsert(obj).then((res) => {
      indicateResult(res, rng);
    }).catch((err) => {
      indicateError(err, rng);
    });
};

const doRemove = () => {
    ({ rng, obj } = prepareObject());
    freeze(true);
    remove(obj).then((res) => {
      indicateResult(`/*${obj}*/`, rng);
    }).catch((err) => {
      indicateError(err, rng);
    });
};

editor.setTheme("ace/theme/chrome");
editor.session.setMode('ace/mode/accounting');
editor.setOption('showLineNumbers', false);
editor.renderer.setShowGutter(true);
editor.commands.removeCommands(['removetolineend', 'removewordright']);
editor.commands.addCommand({
  name: 'upsert',
  bindKey: 'Alt-Enter',
  exec: doUpsert,
  readOnly: false,
});
editor.commands.addCommand({
  name: 'remove',
  bindKey: 'Alt-Delete',
  exec: doRemove,
  readOnly: false,
});
editor.commands.bindKeys({
  'Esc': (e) => { e.cmdLine.focus(); },
});
editor.showCommandLine = (val) => {
  cmdLine.focus();
  if (typeof val == 'string')
    editor.cmdLine.setValue(val, 1);
};

cmdLine.commands.bindKeys({
  'Tab': (e) => { e.editor.focus(); },
  'Shift+Return': (e) => {
    const command = e.getValue();
    freeze(true);
    execute(command).then((res) => {
      finalize(res, true, true);
    }).catch((err) => {
      finalize(err, false, true);
    });
    e.editor.focus();
  },
  'Return': (e) => {
    const command = e.getValue();
    freeze(true);
    execute(command).then((res) => {
      finalize(res, true, command === '');
    }).catch((err) => {
      finalize(err, false, command === '');
    });
    e.editor.focus();
  },
});
cmdLine.commands.removeCommands(['find', 'gotoline', 'findall', 'replace', 'replaceall']);
cmdLine.focus();

document.getElementById('create').onclick = () => {
  freeze(true);
  execute('').then((res) => {
    finalize(res, true, true);
  }).catch((err) => {
    finalize(err, false, true);
  });
  editor.focus();
};

document.getElementById('upsert').onclick = doUpsert;
document.getElementById('remove').onclick = doRemove;
