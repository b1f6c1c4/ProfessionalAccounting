define('ace/mode/folding/accounting', function(require, exports, module) {
  const oop = require('../../lib/oop');
  const Range = require('../../range').Range;
  const BaseFoldMode = require('./fold_mode').FoldMode;

  const FoldMode = function() {
    this.foldingStartMarker = /^(@new [A-Z][a-z]+ )(\{)/;
    this.foldingStopMarker = /\}@$/;
    this.getFoldWidgetRange = function(session, foldStyle, row) {
      const line = session.getLine(row);
      const match = line.match(this.foldingStartMarker);
      if (match) {
        const i = match.index + match[1].length;
        return this.openingBracketBlock(session, match[2], row, i);
      }
    };
  };

  oop.inherits(FoldMode, BaseFoldMode);
  exports.AccountingFoldMode = FoldMode;
});
