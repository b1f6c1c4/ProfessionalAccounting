using System;

namespace ZXing
{
    /// <summary>
    ///     The general exception class throw when something goes wrong during decoding of a barcode.
    ///     This includes, but is not limited to, failing checksums / error correction algorithms, being
    ///     unable to locate finder timing patterns, and so on.
    /// </summary>
    /// <author>Sean Owen</author>
    [Serializable]
    public class ReaderException : Exception
    {
        /// <summary>
        ///     Gets the instance.
        /// </summary>
        public static ReaderException Instance { get { return instance; } }

        // TODO: Currently we throw up to 400 ReaderExceptions while scanning a single 240x240 image before
        // rejecting it. This involves a lot of overhead and memory allocation, and affects both performance
        // and latency on continuous scan clients. In the future, we should change all the decoders not to
        // throw exceptions for routine events, like not finding a barcode on a given row. Instead, we
        // should return error codes back to the callers, and simply delete this class. In the mean time, I
        // have altered this class to be as lightweight as possible, by ignoring the exception string, and
        // by disabling the generation of stack traces, which is especially time consuming. These are just
        // temporary measures, pending the big cleanup.

        //UPGRADE_NOTE: Final was removed from the declaration of 'instance '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private static readonly ReaderException instance = new ReaderException();

        // EXCEPTION TRACKING SUPPORT
        // Identifies who is throwing exceptions and how often. To use:
        //
        // 1. Uncomment these lines and the code below which uses them.
        // 2. Uncomment the two corresponding lines in j2se/CommandLineRunner.decode()
        // 3. Change core to build as Java 1.5 temporarily
        //  private static int exceptionCount = 0;
        //  private static Map<String,Integer> throwers = new HashMap<String,Integer>(32);

        /// <summary>
        ///     Initializes a new instance of the <see cref="ReaderException" /> class.
        /// </summary>
        protected ReaderException()
        {
            // do nothing
        }
    }
}
