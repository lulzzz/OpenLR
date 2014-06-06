﻿using OpenLR.Model;

namespace OpenLR.Binary.Data
{
    /// <summary>
    /// Represents a coordinate convertor that encodes/decodes coordinates into the binary OpenLR format.
    /// </summary>
    public static class CoordinateConverter
    {
        /// <summary>
        /// Decodes binary OpenLR coordinate data into a coordinate.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Coordinate Decode(byte[] data)
        {
            return CoordinateConverter.Decode(data, 0);
        }

        /// <summary>
        /// Decodes binary OpenLR coordinate data into a coordinate.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static Coordinate Decode(byte[] data, int startIndex)
        {
            return new Coordinate()
            {
                Latitude = CoordinateConverter.DecodeDegrees(CoordinateConverter.DecodeInt24(data, startIndex + 3)),
                Longitude = CoordinateConverter.DecodeDegrees(CoordinateConverter.DecodeInt24(data, startIndex + 0))
            };
        }

        /// <summary>
        /// Decodes binary OpenLR relative coordinate data into a coordinate.
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Coordinate DecodeRelative(Coordinate reference, byte[] data)
        {
            return CoordinateConverter.DecodeRelative(reference, data, 0);
        }

        /// <summary>
        /// Decodes binary OpenLR relative coordinate data into a coordinate.
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static Coordinate DecodeRelative(Coordinate reference, byte[] data, int startIndex)
        {
            return new Coordinate()
            {
                Latitude = reference.Latitude + (CoordinateConverter.DecodeInt16(data, startIndex + 2) / 100000.0),
                Longitude = reference.Longitude + (CoordinateConverter.DecodeInt16(data, startIndex + 0) / 100000.0)
            };
        }

        /// <summary>
        /// Decodes a little-endian 24-bit signed integer from the given byte array into a 32-bit signed integer.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        private static int DecodeInt24(byte[] data, int startIndex)
        {
            int result = ((byte)(data[startIndex + 0] & 127) * (1 << 16)) |    // Bottom 8 bits
                (data[startIndex + 1] * (1 << 8)) |    // Next 8 bits, i.e. multiply by 256
                (data[startIndex + 2] * (1 << 0));   // Next 8 bits, i.e. multiply by 65,536
            // take into account the sign-bit.
            if ((data[startIndex + 0] & (1 << 8 - 1)) != 0)
            { // negative!
                return -result;
            }
            return result;
        }

        /// <summary>
        /// Decodes a little-endian 16-bit signed integer from the given byte array into a 32-bit signed integer.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        private static int DecodeInt16(byte[] data, int startIndex)
        {
            int result = (data[startIndex + 0] * (1 << 8)) |    // Bottom 8 bits
                (data[startIndex + 1] * (1 << 0));   // Next 8 bits, i.e. multiply by 65,536
            // take into account the sign-bit.
            if ((data[startIndex + 0] & (1 << 8 - 1)) != 0)
            { // negative!
                return result-65536;
            }
            return result;
        }

        /// <summary>
        /// Decodes an integer-encoded coordinate.
        /// </summary>
        /// <param name="valueInt"></param>
        /// <returns></returns>
        private static double DecodeDegrees(int valueInt)
        {
            return (((valueInt - System.Math.Sign(valueInt) * 0.5) * 360) / 16777216);
        }

        /// <summary>
        /// Decodes the given coordinate into a binary OpenLR coordinate.
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        public static void Encode(Coordinate coordinate, byte[] data, int startIndex)
        {
            CoordinateConverter.EncodeInt24(CoordinateConverter.EncodeDegree(coordinate.Longitude), data, startIndex + 0);
            CoordinateConverter.EncodeInt24(CoordinateConverter.EncodeDegree(coordinate.Latitude), data, startIndex + 3);
        }

        /// <summary>
        /// Decodes the given coorrdinate into a binary OpenLR coordinate.
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="coordinate"></param>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        public static void EncodeRelative(Coordinate reference, Coordinate coordinate, byte[] data, int startIndex)
        {
            CoordinateConverter.EncodeInt16((int)((coordinate.Latitude - reference.Latitude) * 100000.0), data, startIndex + 2);
            CoordinateConverter.EncodeInt16((int)((coordinate.Longitude - reference.Longitude) * 100000.0), data, startIndex + 0);
        }

        /// <summary>
        /// Encodes the given degrees into an integer.
        /// </summary>
        /// <param name="value"></param>
        public static int EncodeDegree(double value)
        {
            return (int)(((value * 16777216) / 360.0) + System.Math.Sign(value) * 0.5);
        }

        /// <summary>
        /// Encodes a 32-bit signed integer into a little-endian 24-bit signed integer into the given byte array.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        public static void EncodeInt24(int value, byte[] data, int startIndex)
        {
            data[startIndex + 0]  = (byte)(value >> 16);
            if (value < 0)
            { // the sign bit.
                data[startIndex + 0] = (byte)(data[startIndex + 0] | (byte)(1 << 8 - 1));
            }
            value = value % (1 << 16);
            data[startIndex + 1] = (byte)(value >> 8);
            value = value % (1 << 8);
            data[startIndex + 2] = (byte)value;
        }

        /// <summary>
        /// Encodes a 32-bit signed integer into a little-endian 16-bit signed integer into the given byte array.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        public static void EncodeInt16(int value, byte[] data, int startIndex)
        {
            data[startIndex + 0] = (byte)(value >> 8);
            if (value < 0)
            { // the sign bit.
                data[startIndex + 0] = (byte)(data[startIndex + 0] | (byte)(1 << 8 - 1));
            }
            value = value % (1 << 8);
            data[startIndex + 1] = (byte)value;
        }
    }
}