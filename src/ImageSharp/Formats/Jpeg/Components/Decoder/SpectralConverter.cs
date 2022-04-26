// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.ImageSharp.Formats.Jpeg.Components.Decoder.ColorConverters;

namespace SixLabors.ImageSharp.Formats.Jpeg.Components.Decoder
{
    /// <summary>
    /// Converter used to convert jpeg spectral data to color pixels.
    /// </summary>
    internal abstract class SpectralConverter
    {
        /// <summary>
        /// Supported scaled spectral block sizes for scaled IDCT decoding.
        /// </summary>
        private static readonly int[] ScaledBlockSizes = new int[]
        {
            // 8 => 1, 1/8 of the original size
            1,

            // 8 => 2, 1/4 of the original size
            2,

            // 8 => 4, 1/2 of the original size
            4,
        };

        /// <summary>
        /// Gets a value indicating whether this converter has converted spectral
        /// data of the current image or not.
        /// </summary>
        protected bool Converted { get; private set; }

        /// <summary>
        /// Injects jpeg image decoding metadata.
        /// </summary>
        /// <remarks>
        /// This is guaranteed to be called only once at SOF marker by <see cref="HuffmanScanDecoder"/>.
        /// </remarks>
        /// <param name="frame"><see cref="JpegFrame"/> instance containing decoder-specific parameters.</param>
        /// <param name="jpegData"><see cref="IRawJpegData"/> instance containing decoder-specific parameters.</param>
        public abstract void InjectFrameData(JpegFrame frame, IRawJpegData jpegData);

        /// <summary>
        /// Converts single spectral jpeg stride to color stride in baseline
        /// decoding mode.
        /// </summary>
        /// <remarks>
        /// Called once per decoded spectral stride in <see cref="HuffmanScanDecoder"/>
        /// only for baseline interleaved jpeg images.
        /// Spectral 'stride' doesn't particularly mean 'single stride'.
        /// Actual stride height depends on the subsampling factor of the given image.
        /// </remarks>
        public abstract void ConvertStrideBaseline();

        /// <summary>
        /// Marks current converter state as 'converted'.
        /// </summary>
        /// <remarks>
        /// This must be called only for baseline interleaved jpeg's.
        /// </remarks>
        public void CommitConversion()
        {
            DebugGuard.IsFalse(this.Converted, nameof(this.Converted), $"{nameof(this.CommitConversion)} must be called only once");

            this.Converted = true;
        }

        /// <summary>
        /// Gets the color converter.
        /// </summary>
        /// <param name="frame">The jpeg frame with the color space to convert to.</param>
        /// <param name="jpegData">The raw JPEG data.</param>
        /// <returns>The color converter.</returns>
        protected virtual JpegColorConverterBase GetColorConverter(JpegFrame frame, IRawJpegData jpegData) => JpegColorConverterBase.GetConverter(jpegData.ColorSpace, frame.Precision);

        /// <summary>
        /// Calculates image size with optional scaling.
        /// </summary>
        /// <remarks>
        /// Does not apply scalling if <paramref name="targetSize"/> is null.
        /// </remarks>
        /// <param name="size">Size of the image.</param>
        /// <param name="targetSize">Target size of the image.</param>
        /// <param name="blockPixelSize">Spectral block size, equals to 8 if scaling is not applied.</param>
        /// <returns>Resulting image size, equals to <paramref name="size"/> if scaling is not applied.</returns>
        public static Size CalculateResultingImageSize(Size size, Size? targetSize, out int blockPixelSize)
        {
            const int blockNativePixelSize = 8;

            blockPixelSize = blockNativePixelSize;
            if (targetSize != null)
            {
                Size tSize = targetSize.Value;

                int fullBlocksWidth = (int)((uint)size.Width / blockNativePixelSize);
                int fullBlocksHeight = (int)((uint)size.Height / blockNativePixelSize);

                int blockWidthRemainder = size.Width & (blockNativePixelSize - 1);
                int blockHeightRemainder = size.Height & (blockNativePixelSize - 1);

                for (int i = 0; i < ScaledBlockSizes.Length; i++)
                {
                    int blockSize = ScaledBlockSizes[i];
                    int scaledWidth = (fullBlocksWidth * blockSize) + (int)Numerics.DivideCeil((uint)(blockWidthRemainder * blockSize), blockNativePixelSize);
                    int scaledHeight = (fullBlocksHeight * blockSize) + (int)Numerics.DivideCeil((uint)(blockHeightRemainder * blockSize), blockNativePixelSize);

                    if (scaledWidth >= tSize.Width && scaledHeight >= tSize.Height)
                    {
                        blockPixelSize = blockSize;
                        return new Size(scaledWidth, scaledHeight);
                    }
                }
            }

            return size;
        }
    }
}
