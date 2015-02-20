grammar Console;

@parser::members
{
	protected const int EOF = Eof;
}

@lexer::members
{
	protected const int EOF = Eof;
	protected const int HIDDEN = Hidden;
}

/*
 * Parser Rules
 */

command
	:	(voucherQuery | groupedQuery | asset | amort | otherCommand) EOF
	;

otherCommand
	:	Check | Titles | Launch | Connect | Shutdown | Backup | Mobile | Fetch | Help | Exit
	;
	
asset
	:	assetList | assetQuery | assetRegister | assetUnregister | assetResetSoft | assetResetHard | assetApply | assetCheck
	;
assetList
	:	'a' Modifier=(AOAll | AOList)? rangePoint? aoQ?
	;
assetQuery
	:	'a' AOQuery aoQ?
	;
assetRegister
	:	'a' AORegister aoQ? (':' voucherQuery)?
	;
assetUnregister
	:	'a' AOUnregister aoQ? range?
	;
assetRedep
	:	'a' ARedep aoQ?
	;
assetResetSoft
	:	'a' AOResetSoft aoQ? range?
	;
assetResetHard
	:	'a' AOResetHard aoQ? (':' voucherQuery)?
	;
assetApply
	:	'a' AOApply Collapse=AOCollapse? aoQ? range?
	;
assetCheck
	:	'a' AOCheck aoQ?
	;
amort
	:	amortList | amortQuery | amortRegister | amortUnregister | amortResetSoft | amortResetHard | amortApply | amortCheck
	;
amortList
	:	'o' Modifier=(AOAll | AOList)? rangePoint? aoQ?
	;
amortQuery
	:	'o' AOQuery aoQ?
	;
amortRegister
	:	'o' AORegister aoQ? (':' voucherQuery)?
	;
amortUnregister
	:	'o' AOUnregister aoQ? range?
	;
amortReamo
	:	'o' OReamo aoQ?
	;
amortResetSoft
	:	'o' AOResetSoft aoQ? range?
	;
amortResetHard
	:	'o' AOResetHard aoQ? (':' voucherQuery)?
	;
amortApply
	:	'o' AOApply Collapse=AOCollapse? aoQ? range?
	;
amortCheck
	:	'o' AOCheck aoQ?
	;

aoQ
	:	(aoQAtom ('+' aoQAtom)*)
	;

aoQAtom
	:	SingleQuotedString | DoubleQuotedString | Guid
	;

groupedQuery
	:	voucherQuery subtotal
	;

subtotal
	:	SubtotalMark=('`' | '``' | '!' | '!!') SubtotalFields=('t' | '-' | 'c' | 'r' | 'd')* AggregationMethod=('D' | 'x' | 'X')?
	;

voucherQuery
	:	details? range? VoucherRemark?
	;

details
	:	detailsX (Operator=('+' | '-') detailsX)*
	;

detailsX
	:	detailAtom (Operator='*' detailAtom)*
	;

detailAtom
	:	'(' details ')'
	|	detailUnary
	;

detailUnary
	:	op='-' detailQuery
	|	detailQuery
	;

detailQuery
	:	(DetailTitle | DetailTitleSubTitle)? SingleQuotedString? DoubleQuotedString? Dir=('d' | 'c')?
	;

range
	:	RangeNull | RangeAllNotNull | RangeAllNotNull | RangeAll
	|	RangeDays | RangeWeeks
	|	RangeFMonth | RangeOneFMonth | RangeMultiFMonth | RangeFromFMonth | RangeToFMonth | RangeToFMonth
	|	RangeMonth | RangeOneMonth | RangeMultiMonth | RangeFromMonth | RangeToMonth | RangeToMonth
	|	RangeBMonth | RangeOneBMonth | RangeMultiBMonth | RangeFromBMonth | RangeToBMonth | RangeToBMonth
	;

rangePoint
	:	RangeNull | RangeAll
	|	RangeDays | RangeWeeks
	|	RangeFMonth | RangeOneFMonth
	|	RangeMonth | RangeOneMonth
	|	RangeBMonth | RangeOneBMonth
	;
	
/*
 * Lexer Rules
 */

Launch
	:	'lan'
	|	'launch'
	;
Connect
	:	'con'
	|	'connect'
	;
Shutdown
	:	'shu'
	|	'shutdown'
	;
Backup
	:	'backup'
	;
Mobile
	:	'mob'
	|	'mobile'
	;
Fetch
	:	'fetch'
	;
Help
	:	'help'
	|	'?'
	;
Titles
	:	'titles'
	|	'T' | 't'
	;
Exit
	:	'exit'
	;
Check
	:	'chk' [1-2]
	;
	
AOAll
	:	'-all'
	;
AOList
	:	'-li'
	|	'-list'
	;
AOQuery
	:	'-q'
	|	'-query'
	;
AORegister
	:	'-reg'
	|	'-register'
	;
AOUnregister
	:	'-unr'
	|	'-unregister'
	;
ARedep
	:	'-rd'
	|	'-redep'
	;
OReamo
	:	'-ra'
	|	'-reamo'
	;
AOResetSoft
	:	'-reset-soft'
	;
AOResetHard
	:	'-reset-hard'
	;
AOApply
	:	'-ap'
	|	'-apply'
	;
AOCollapse
	:	'-co'
	|	'-collapse'
	;
AOCheck
	:	'-chk'
	;

Guid
	:	H H H H H H H H '-' H H H H '-' H H H H '-' H H H H '-' H H H H H H H H H H H H
	;

fragment H
	:	[0-9A-Za-z] 
	;

RangeNull
	:	'[null]'
	|	'null'
	;
RangeAllNotNull
	:	'[~null]'
	|	'~null'
	;
RangeAll
	:	'[]'
	;

RangeDays
	:	'[' '.'+ ']'
	|	'.'+
	;
RangeWeeks
	:	'[' '-'+ ']'
	|	'-'+
	;

RangeFMonth
	:	'[0]'
	|	'0'
	|	'[-' [1-9] [0-9]* ']'
	|	'-' [1-9] [0-9]*
	|	'[+' [1-9] [0-9]* ']'
	|	'+' [1-9] [0-9]*
	;
RangeOneFMonth
	:	'[' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] ']'
	|	[1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;
RangeMultiFMonth
	:	'[' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] '~' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] ']'
	|	[1-2] [0-9] [0-9] [0-9] [0-1] [0-9] '~' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;
RangeFromFMonth
	:	'[' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] '~]'
	|	[1-2] [0-9] [0-9] [0-9] [0-1] [0-9] '~'
	;
RangeToFMonth
	:	'[~' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] ']'
	|	'~' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;
RangeXToFMonth
	:	'[-' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] ']'
	|	'-' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;
	
RangeMonth
	:	'[@0]'
	|	'@0'
	|	'[@-' [1-9] [0-9]* ']'
	|	 '@-' [1-9] [0-9]*
	|	'[@+' [1-9] [0-9]* ']'
	|	'@+' [1-9] [0-9]*
	;
RangeOneMonth
	:	'[@' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] ']'
	|	'@' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;
RangeMultiMonth
	:	'[@' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] '~@' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] ']'
	|	'@' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] '~@' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;
RangeFromMonth
	:	'[@' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] '~]'
	|	'@' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] '~'
	;
RangeToMonth
	:	'[~@' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] ']'
	|	'~@' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;
RangeXToMonth
	:	'[-@' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] ']'
	|	'-@' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;
	
RangeBMonth
	:	'[#0]'
	|	'#0'
	|	'[#-' [1-9] [0-9]* ']'
	|	 '#-' [1-9] [0-9]*
	|	'[#+' [1-9] [0-9]* ']'
	|	'#+' [1-9] [0-9]*
	;
RangeOneBMonth
	:	'[#' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] ']'
	|	'#' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;
RangeMultiBMonth
	:	'[#' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] '~@' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] ']'
	|	'#' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] '~@' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;
RangeFromBMonth
	:	'[#' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] '~]'
	|	'#' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] '~'
	;
RangeToBMonth
	:	'[~#' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] ']'
	|	'~#' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;
RangeXToBMonth
	:	'[-#' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] ']'
	|	'-#' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;

RangeOneDay
	:	'[' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9] ']'
	|	[1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9]
	;
RangeMultiDay
	:	'[' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9] '~' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9] ']'
	|	[1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9] '~' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9]
	;
RangeFromDay
	:	'[' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9] '~]'
	|	[1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9] '~'
	;
RangeToDay
	:	'[~' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9] ']'
	|	'~' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9]
	;
RangeXToDay
	:	'[-' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9] ']'
	|	'-' [1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9]
	;

VoucherRemark
	:	'%' ('%%'|~('%'))* '%'
	;
	
DoubleQuotedString
	:	'"' ('""'|~('"'))* '"'
	;

SingleQuotedString
	:	'\'' ('\'\''|~('\''))* '\''
	;

DetailTitle
	:	'T' [1-9] [0-9] [0-9] [0-9]
	;

DetailTitleSubTitle
	:	'T' [1-9] [0-9] [0-9] [0-9] [0-9] [0-9]
	;

WS
	:	' ' -> skip
	;
