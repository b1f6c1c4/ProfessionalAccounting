grammar Subtotal;

/*
 * Parser Rules
 */

subtotal
	:	Mark=SumMark SubtotalFields? subtotalAggr? subtotalEqui?
	|	Mark=CountMark SubtotalFields? subtotalAggr?
	;

subtotalAggr
	:	AggrMark AllDate?
	|	AggrMark SquareBra rangeCore SquareKet
	;

subtotalEqui
	:	EquiMark VoucherCurrency? (SquareBra rangeDay SquareKet)?
	;

rangeCore
	:	RangeNull | RangeAllNotNull
	|	Begin=rangeCertainPoint Tilde End=rangeCertainPoint?
	|	Tilde End=rangeCertainPoint
	|	Certain=rangeCertainPoint
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

/*
 * Lexer Rules
 */

SumMark
	:	'`' | '``'
	;

CountMark
	:	'!'
	;

SubtotalFields
	:	[tscrdwmyC]*
	|	'v'
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

VoucherCurrency
	:	'@' [a-zA-Z]+
	|	'@@'
	;

SquareBra
	:	'['
	;
SquareKet
	:	']'
	;

AllDate
	:	'[]'
	;

Tilde
	:	'~' | '~~'
	;

AggrMark
	:	'D'
	;
EquiMark
	:	'X'
	;

WS
	:	[ \n\r\t] -> channel(HIDDEN)
	;
