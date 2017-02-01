grammar Query;

/*
 * Parser Rules
 */

groupedQuery
	:	voucherDetailQuery subtotal
	;

subtotal
	:	SubtotalMark=('`' | '``' | '!') SubtotalFields? subtotalAggr?
	;

subtotalAggr
	:	'D' IsAll='[]'?
	|	'D' '[' rangeCore ']'
	;

voucherDetailQuery
	:	vouchers emit
	|	voucherQuery
	;

emit
	:	Op='A' | ':' details
	;

vouchers
	:	vouchersB
	|	voucherQuery
	;

vouchersB
	:	vouchersB Op='*' vouchersB
	|	vouchersB Op=('+' | '-') vouchersB
	|	Op=('+' | '-') vouchersB
	|	'{' voucherQuery '}'
	|	'{' vouchersB '}'
	;

voucherQuery
	:	details? Op=('A' | 'E')? range? CaretQuotedString? PercentQuotedString? VoucherType? VoucherCurrency?
	;

details
	:	details Op='*' details
	|	details Op=('+' | '-') details
	|	Op=('+' | '-') details
	|	detailQuery
	|	'(' details ')'
	;

detailQuery
	:	title? SingleQuotedString? DoubleQuotedString? Direction=('>' | '<')?
	;

title
	:	DetailTitle | DetailTitleSubTitle
	;

range
	:	'[]' | Core=rangeCore | '[' Core=rangeCore ']'
	;

uniqueTime
	:	Core=uniqueTimeCore | '[' Core=uniqueTimeCore ']'
	;

rangeCore
	:	RangeNull | RangeAllNotNull
	|	Begin=rangeCertainPoint Op=('~'|'=') End=rangeCertainPoint?
	|	Op=('~'|'=') End=rangeCertainPoint
	|	Certain=rangeCertainPoint
	;

uniqueTimeCore
	:	RangeNull
	|	Day=rangeDay
	;

rangePoint
	:	RangeNull | All='[]'
	|	rangeCertainPoint
	;

rangeCertainPoint
	:	rangeYear
	|	rangeMonth
	|	rangeWeek
	|	rangeDay
	;

rangeYear
	:	RangeAYear
	;

rangeMonth
	:	(RangeAMonth | RangeDeltaMonth)
	;

rangeWeek
	:	RangeDeltaWeek
	;

rangeDay
	:	RangeADay | RangeDeltaDay
	;

distributedQ
	:	distributedQ Op='*' distributedQ
	|	distributedQ Op=('+' | '-') distributedQ
	|	Op=('+' | '-') distributedQ
	|	distributedQAtom
	|	'(' distributedQ ')'
	;
distributedQAtom
	:	Guid? DollarQuotedString? PercentQuotedString? ('[[' rangeCore ']]')?
	;

/*
 * Lexer Rules
 */

SubtotalFields
	:	('t' | 's' | 'c' | 'r' | 'd' | 'w' | 'm' | 'y' | 'C')+
	|	'v'
	;

Guid
	:	H H H H H H H H '-' H H H H '-' H H H H '-' H H H H '-' H H H H H H H H H H H H
	;

fragment H
	:	[0-9A-Za-z]
	;

RangeNull
	:	'null'
	;
RangeAllNotNull
	:	'~null'
	;

RangeAYear
	:	[1-2] [0-9] [0-9] [0-9]
	;
RangeAMonth
	:	[1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;
RangeDeltaMonth
	:	'0'
	|	'-' [1-9] [0-9]*
	;
RangeADay
	:	[1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9]
	;
RangeDeltaDay
	:	'.'+
	;
RangeDeltaWeek
	:	','+
	;

VoucherType
	:	'Ordinary' | 'G' | 'General' | 'Carry' | 'Amortization' | 'Depreciation' | 'Devalue' | 'AnnualCarry' | 'Uncertain'
	;

VoucherCurrency
	:	'@' [a-zA-Z]+
	|	'@@'
	;

CaretQuotedString
	:	'^' ('^^'|~('^'))* '^'
	;

PercentQuotedString
	:	'%' ('%%'|~('%'))* '%'
	;

DollarQuotedString
	:	'$' ('$$'|~('$'))* '$'
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

Intersect
	: '*'
	;
Union
	: '+'
	;
Substract
	: '-'
	;

WS
	:	' ' -> channel(HIDDEN)
	;
