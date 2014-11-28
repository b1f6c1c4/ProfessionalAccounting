using System;
using System.Collections.Generic;
using ZXing.Multi.QrCode.Internal;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace ZXing.Multi.QrCode
{
    /// <summary>
    ///     This implementation can detect and decode multiple QR Codes in an image.
    /// </summary>
    public sealed class QRCodeMultiReader : QRCodeReader, MultipleBarcodeReader
    {
        private static readonly ResultPoint[] NO_POINTS = new ResultPoint[0];

        /// <summary>
        ///     Decodes the multiple.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <returns></returns>
        public Result[] decodeMultiple(BinaryBitmap image)
        {
            return decodeMultiple(image, null);
        }

        /// <summary>
        ///     Decodes the multiple.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="hints">The hints.</param>
        /// <returns></returns>
        public Result[] decodeMultiple(BinaryBitmap image, IDictionary<DecodeHintType, object> hints)
        {
            var results = new List<Result>();
            var detectorResults = new MultiDetector(image.BlackMatrix).detectMulti(hints);
            foreach (var detectorResult in detectorResults)
            {
                var decoderResult = getDecoder().decode(detectorResult.Bits, hints);
                if (decoderResult == null)
                    continue;

                var points = detectorResult.Points;
                // If the code was mirrored: swap the bottom-left and the top-right points.
                var data = decoderResult.Other as QRCodeDecoderMetaData;
                if (data != null)
                    data.applyMirroredCorrection(points);
                var result = new Result(decoderResult.Text, decoderResult.RawBytes, points, BarcodeFormat.QR_CODE);
                var byteSegments = decoderResult.ByteSegments;
                if (byteSegments != null)
                    result.putMetadata(ResultMetadataType.BYTE_SEGMENTS, byteSegments);
                var ecLevel = decoderResult.ECLevel;
                if (ecLevel != null)
                    result.putMetadata(ResultMetadataType.ERROR_CORRECTION_LEVEL, ecLevel);
                if (decoderResult.StructuredAppend)
                {
                    result.putMetadata(
                                       ResultMetadataType.STRUCTURED_APPEND_SEQUENCE,
                                       decoderResult.StructuredAppendSequenceNumber);
                    result.putMetadata(
                                       ResultMetadataType.STRUCTURED_APPEND_PARITY,
                                       decoderResult.StructuredAppendParity);
                }
                results.Add(result);
            }
            if (results.Count == 0)
                return null;
            results = ProcessStructuredAppend(results);
            return results.ToArray();
        }

        private List<Result> ProcessStructuredAppend(List<Result> results)
        {
            var hasSA = false;
            // first, check, if there is at least on SA result in the list
            foreach (var result in results)
                if (result.ResultMetadata.ContainsKey(ResultMetadataType.STRUCTURED_APPEND_SEQUENCE))
                {
                    hasSA = true;
                    break;
                }
            if (!hasSA)
                return results;
            // it is, second, split the lists and built a new result list
            var newResults = new List<Result>();
            var saResults = new List<Result>();
            foreach (var result in results)
            {
                newResults.Add(result);
                if (result.ResultMetadata.ContainsKey(ResultMetadataType.STRUCTURED_APPEND_SEQUENCE))
                    saResults.Add(result);
            }
            // sort and concatenate the SA list items
            saResults.Sort(SaSequenceSort);
            var concatedText = String.Empty;
            var rawBytesLen = 0;
            var byteSegmentLength = 0;
            foreach (var saResult in saResults)
            {
                concatedText += saResult.Text;
                rawBytesLen += saResult.RawBytes.Length;
                if (saResult.ResultMetadata.ContainsKey(ResultMetadataType.BYTE_SEGMENTS))
                    foreach (
                        var segment in (IEnumerable<byte[]>)saResult.ResultMetadata[ResultMetadataType.BYTE_SEGMENTS])
                        byteSegmentLength += segment.Length;
            }
            var newRawBytes = new byte[rawBytesLen];
            var newByteSegment = new byte[byteSegmentLength];
            var newRawBytesIndex = 0;
            var byteSegmentIndex = 0;
            foreach (var saResult in saResults)
            {
                Array.Copy(saResult.RawBytes, 0, newRawBytes, newRawBytesIndex, saResult.RawBytes.Length);
                newRawBytesIndex += saResult.RawBytes.Length;
                if (saResult.ResultMetadata.ContainsKey(ResultMetadataType.BYTE_SEGMENTS))
                    foreach (
                        var segment in (IEnumerable<byte[]>)saResult.ResultMetadata[ResultMetadataType.BYTE_SEGMENTS])
                    {
                        Array.Copy(segment, 0, newByteSegment, byteSegmentIndex, segment.Length);
                        byteSegmentIndex += segment.Length;
                    }
            }
            var newResult = new Result(concatedText, newRawBytes, NO_POINTS, BarcodeFormat.QR_CODE);
            if (byteSegmentLength > 0)
            {
                var byteSegmentList = new List<byte[]>();
                byteSegmentList.Add(newByteSegment);
                newResult.putMetadata(ResultMetadataType.BYTE_SEGMENTS, byteSegmentList);
            }
            newResults.Add(newResult);
            return newResults;
        }

        private int SaSequenceSort(Result a, Result b)
        {
            var aNumber = (int)(a.ResultMetadata[ResultMetadataType.STRUCTURED_APPEND_SEQUENCE]);
            var bNumber = (int)(b.ResultMetadata[ResultMetadataType.STRUCTURED_APPEND_SEQUENCE]);
            return aNumber - bNumber;
        }
    }
}
