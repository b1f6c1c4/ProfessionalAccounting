grammar Query;

/*
 * Parser Rules
 */

groupedQuery
	:	voucherDetailQuery subtotal
	;

subtotal
	:	SubtotalMark=('`' | '``') SubtotalFields? subtotalAggr? subtotalEqui?
	|	SubtotalMark='!' SubtotalFields? subtotalAggr?
	;

subtotalAggr
	:	'D' IsAll='[]'?
	|	'D' '[' rangeCore ']'
	;

subtotalEqui
	:	'X' ('[' rangeDay ']')?
	;

voucherDetailQuery
	:	vouchers emit
	|	voucherQuery
	;

emit
	:	':' details
	;

vouchers
	:	vouchers2
	|	voucherQuery
	;

vouchers2
	:	vouchers2 Op=('+' | '-') vouchers1
	|	Op=('+' | '-')? vouchers1
	;

vouchers1
	:	vouchers0 (Op='*' vouchers1)?
	;

vouchers0
	:	'{' voucherQuery '}'
	|	'{' vouchers2 '}'
	;

voucherQuery
	:	details? Op='A'? range? CaretQuotedString? PercentQuotedString? VoucherType?
	;

details
	:	details Op=('+' | '-') details1
	|	Op=('+' | '-')? details1
	;

details1
	:	details0 (Op='*' details1)?
	;

details0
	:	detailQuery
	|	'(' details ')'
	;

detailQuery
	:	VoucherCurrency? title? SingleQuotedString? DoubleQuotedString? Floating? Direction=('>' | '<')?
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
	|	Begin=rangeCertainPoint Op=('~'|'~~') End=rangeCertainPoint?
	|	Op=('~'|'~~') End=rangeCertainPoint
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
	:	distributedQ Op=('+' | '-') distributedQ1
	|	Op=('+' | '-')? distributedQ1
	;

distributedQ1
	:	distributedQ0 (Op='*' distributedQ1)?
	;

distributedQ0
	:	distributedQAtom
	|	'(' distributedQ ')'
	;

distributedQAtom
	:	Guid? DollarQuotedString? PercentQuotedString? ('[[' rangeCore ']]')?
	;

/*
 * Lexer Rules
 */

Floating
	:	'=' Sign='-'? [1-9] ([0-9])* ('.' [0-9]+)?
	|	'=' Sign='-'? '0' '.' [0-9]+
	;

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
