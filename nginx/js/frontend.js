/* Copyright (C) 2020-2021 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

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
};

const finalize = (answer, success, insert) => {
  if (insert) {
    editor.selection.setSelectionRange({
      start: { row: editor.selection.getCursor().row, column: 0 },
      end: { row: editor.selection.getCursor().row, column: 0 },
    }, false);
  } else {
    editor.setValue('');
  }
  const original = editor.selection.getCursor();
  editor.insert(answer);
  editor.selection.setSelectionRange({
    start: original,
    end: original,
  }, false);
  freeze(false);
  if (success) {
    cmdLine.selectAll();
  } else {
    console.error(answer);
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

const doCreate = () => {
  freeze(true);
  execute('').then((res) => {
    finalize(res, true, true);
    editor.renderer.scrollCursorIntoView();
    editor.selection.setSelectionRange({
      start: { row: editor.selection.getCursor().row + 1, column: 0 },
      end: { row: editor.selection.getCursor().row + 1, column: 0 },
    }, false);
  }).catch((err) => {
    finalize(err, false, true);
  });
  editor.focus();
};

const doUpsert = () => {
  const { rng, obj } = prepareObject();
  freeze(true);
  upsert(obj).then((res) => {
    indicateResult(res, rng);
  }).catch((err) => {
    indicateError(err, rng);
  });
};

const doUpsertAll = async () => {
  let rid = 0;
  while (true) {
    editor.selection.setSelectionRange({
      start: { row: rid, column: 0 },
      end: { row: rid, column: 0 },
    }, false);
    const { rng, obj } = prepareObject();
    if (rng.end.row <= rid) {
      break;
    }
    rid = rng.end.row;
    try {
      freeze(true);
      const res = await upsert(obj);
      indicateResult(res, rng);
    } catch (err) {
      indicateError(err, rng);
      break;
    }
  }
};

const doRemove = () => {
  const { rng, obj } = prepareObject();
  freeze(true);
  remove(obj).then((res) => {
    indicateResult(`/*${obj}*/`, rng);
  }).catch((err) => {
    indicateError(err, rng);
  });
};

const doRemoveAll = async () => {
  let rid = 0;
  while (true) {
    editor.selection.setSelectionRange({
      start: { row: rid, column: 0 },
      end: { row: rid, column: 0 },
    }, false);
    const { rng, obj } = prepareObject();
    if (rng.end.row <= rid) {
      break;
    }
    rid = rng.end.row;
    try {
      freeze(true);
      const res = await remove(obj);
      indicateResult(res, rng);
    } catch (err) {
      indicateError(err, rng);
      break;
    }
  }
};

const doUpload = () => {
  const command = cmdLine.getValue();
  const text = editor.getValue();
  const combined = command + '\n' + text;
  freeze(true);
  execute(combined).then((res) => {
    finalize(res, true, false);
    editor.focus();
    editor.renderer.scrollCursorIntoView();
  }).catch((err) => {
    finalize(err, false, false);
    editor.renderer.scrollCursorIntoView();
  });
};

const doExecuteFactory = (app) => () => {
  const command = cmdLine.getValue();
  if (command === '') {
    doCreate();
    return;
  }
  freeze(true);
  execute(command).then((res) => {
    finalize(res, true, app);
    editor.focus();
    editor.renderer.scrollCursorIntoView();
  }).catch((err) => {
    finalize(err, false, app);
    editor.renderer.scrollCursorIntoView();
  });
};

editor.setTheme("ace/theme/chrome");
editor.session.setMode('ace/mode/accounting');
editor.setOption('showLineNumbers', false);
editor.renderer.setShowGutter(true);
editor.commands.addCommands([{
  name: 'upsert',
  bindKey: 'Alt-Enter',
  exec: doUpsert,
  readOnly: false,
}, {
  name: 'remove',
  bindKey: 'Alt-Delete',
  exec: doRemove,
  readOnly: false,
}, {
  name: 'upload',
  bindKey: 'Ctrl+Alt+Enter',
  exec: doUpload,
  readOnly: false,
}, {
  name: 'upsertAll',
  bindKey: 'Ctrl+Alt+Shift+Enter',
  exec: doUpsertAll,
  readOnly: false,
}, {
  name: 'removeAll',
  bindKey: 'Ctrl+Alt+Shift+Delete',
  exec: doRemoveAll,
  readOnly: false,
}]);
editor.commands.bindKeys({
  'Tab': () => { cmdLine.focus(); },
});
editor.insert(notice);

cmdLine.commands.bindKeys({
  'Tab': () => { editor.focus(); },
  'Ctrl+Alt+Enter': doUpload,
  'Shift+Enter': doExecuteFactory(true),
  'Enter': doExecuteFactory(false),
});
cmdLine.commands.removeCommands(['find', 'gotoline', 'findall', 'replace', 'replaceall']);
cmdLine.focus();
