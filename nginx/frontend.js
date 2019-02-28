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

const freeze = (f) => {
  editor.setReadOnly(f);
  cmdLine.setReadOnly(f);
};

editor.renderer.setShowGutter(false);
editor.commands.addCommand({
  name: 'upsert',
  bindKey: 'Alt-Enter',
  exec: () => console.log('upsert'), // TODO
  readOnly: false,
}, {
  name: 'remove',
  bindKey: 'Alt-Delete',
  exec: () => console.log('remove'), // TODO
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
      finalize(res, false, true);
    });
    e.editor.focus();
  },
  'Return': (e) => {
    const command = e.getValue();
    freeze(true);
    execute(command).then((res) => {
      finalize(res, true, command === '');
    }).catch((err) => {
      finalize(res, false, command === '');
    });
    e.editor.focus();
  },
});
cmdLine.commands.removeCommands(['find', 'gotoline', 'findall', 'replace', 'replaceall']);
cmdLine.focus();
