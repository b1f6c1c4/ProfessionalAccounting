define('ace/mode/accounting', function(require, exports, module) {
  const oop = require('../lib/oop');
  const TextMode = require('./text').Mode;

  const AccountingHighlightRules = require('./accounting_highlight_rules').AccountingHighlightRules;
  const AccountingFoldMode = require('./folding/accounting').AccountingFoldMode;

  const Mode = function() {
    this.HighlightRules = AccountingHighlightRules;
    this.foldingRules = new AccountingFoldMode();
  };
  oop.inherits(Mode, TextMode);

  Mode.prototype.lineCommentStart = '//';

  exports.Mode = Mode;
});
