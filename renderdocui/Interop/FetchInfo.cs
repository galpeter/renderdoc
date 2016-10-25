/******************************************************************************
 * The MIT License (MIT)
 * 
 * Copyright (c) 2015-2016 Baldur Karlsson
 * Copyright (c) 2014 Crytek
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 ******************************************************************************/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using renderdocui.Code;

namespace renderdoc
{
    //////////////////////////// BEGIN HACK /////////////////////////////////

    [Flags]
    public enum VkAccessFlags
    {
        INDIRECT_COMMAND_READ_BIT = 0x00000001,
        INDEX_READ_BIT = 0x00000002,
        VERTEX_ATTRIBUTE_READ_BIT = 0x00000004,
        UNIFORM_READ_BIT = 0x00000008,
        INPUT_ATTACHMENT_READ_BIT = 0x00000010,
        SHADER_READ_BIT = 0x00000020,
        SHADER_WRITE_BIT = 0x00000040,
        COLOR_ATTACHMENT_READ_BIT = 0x00000080,
        COLOR_ATTACHMENT_WRITE_BIT = 0x00000100,
        DEPTH_STENCIL_ATTACHMENT_READ_BIT = 0x00000200,
        DEPTH_STENCIL_ATTACHMENT_WRITE_BIT = 0x00000400,
        TRANSFER_READ_BIT = 0x00000800,
        TRANSFER_WRITE_BIT = 0x00001000,
        HOST_READ_BIT = 0x00002000,
        HOST_WRITE_BIT = 0x00004000,
        MEMORY_READ_BIT = 0x00008000,
        MEMORY_WRITE_BIT = 0x00010000,
    };

    [Flags]
    public enum VkImageAspectFlags
    {
        COLOR_BIT = 0x00000001,
        DEPTH_BIT = 0x00000002,
        STENCIL_BIT = 0x00000004,
        METADATA_BIT = 0x00000008,
    };

    public enum VkImageLayout
    {
        UNDEFINED = 0,
        GENERAL = 1,
        COLOR_ATTACHMENT_OPTIMAL = 2,
        DEPTH_STENCIL_ATTACHMENT_OPTIMAL = 3,
        DEPTH_STENCIL_READ_ONLY_OPTIMAL = 4,
        SHADER_READ_ONLY_OPTIMAL = 5,
        TRANSFER_SRC_OPTIMAL = 6,
        TRANSFER_DST_OPTIMAL = 7,
        PREINITIALIZED = 8,
        PRESENT_SRC_KHR = 1000001002,
    };

    [Flags]
    public enum VkPipelineStageFlags
    {
        TOP_OF_PIPE_BIT = 0x00000001,
        DRAW_INDIRECT_BIT = 0x00000002,
        VERTEX_INPUT_BIT = 0x00000004,
        VERTEX_SHADER_BIT = 0x00000008,
        TESSELLATION_CONTROL_SHADER_BIT = 0x00000010,
        TESSELLATION_EVALUATION_SHADER_BIT = 0x00000020,
        GEOMETRY_SHADER_BIT = 0x00000040,
        FRAGMENT_SHADER_BIT = 0x00000080,
        EARLY_FRAGMENT_TESTS_BIT = 0x00000100,
        LATE_FRAGMENT_TESTS_BIT = 0x00000200,
        COLOR_ATTACHMENT_OUTPUT_BIT = 0x00000400,
        COMPUTE_SHADER_BIT = 0x00000800,
        TRANSFER_BIT = 0x00001000,
        BOTTOM_OF_PIPE_BIT = 0x00002000,
        HOST_BIT = 0x00004000,
        ALL_GRAPHICS_BIT = 0x00008000,
        ALL_COMMANDS_BIT = 0x00010000,
    };

    [StructLayout(LayoutKind.Sequential)]
    public class VkImageSubresourceRange
    {
        public static UInt32 VK_REMAINING_MIP_LEVELS = UInt32.MaxValue;
        public static UInt32 VK_REMAINING_ARRAY_LAYERS = UInt32.MaxValue;

        public VkImageAspectFlags aspectMask = VkImageAspectFlags.COLOR_BIT;
        public UInt32 baseMipLevel;
        public UInt32 levelCount = VK_REMAINING_MIP_LEVELS;
        public UInt32 baseArrayLayer;
        public UInt32 layerCount = VK_REMAINING_ARRAY_LAYERS;

        public override string ToString()
        {
            return String.Format("{0} (mips {1} - {2}) (layers {3} - {4})", aspectMask, baseMipLevel, levelCount == VK_REMAINING_MIP_LEVELS ? "ALL" : levelCount.ToString(), baseArrayLayer, layerCount == VK_REMAINING_ARRAY_LAYERS ? "ALL" : layerCount.ToString());
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public class VkMemoryBarrier
    {
        private UInt32 sType = 46; // VK_STRUCTURE_TYPE_MEMORY_BARRIER
        private UIntPtr pNext = UIntPtr.Zero;
        VkAccessFlags      srcAccessMask;
        VkAccessFlags      dstAccessMask;

        public override string ToString()
        {
            return String.Format("VkMemoryBarrier[ src: {0} dst: {1} ]", srcAccessMask, dstAccessMask);
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public class VkBufferMemoryBarrier
    {
        public static UInt64 VK_WHOLE_SIZE = UInt64.MaxValue;

        private UInt32 sType = 44; // VK_STRUCTURE_TYPE_BUFFER_MEMORY_BARRIER
        private UIntPtr pNext = UIntPtr.Zero;
        VkAccessFlags srcAccessMask;
        VkAccessFlags dstAccessMask;
        private UInt32 srcQueueFamilyIndex = 0;
        private UInt32 dstQueueFamilyIndex = 0;
        public ResourceId buffer;
        public UInt64 offset;
        public UInt64 size = VK_WHOLE_SIZE;

        public override string ToString()
        {
            return String.Format("VkMemoryBarrier[ src: {0} dst: {1} buf: {2} offs: {3} size: {4}]", srcAccessMask, dstAccessMask, buffer, offset, size == VK_WHOLE_SIZE ? "VK_WHOLE_SIZE" : size.ToString());
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public class VkImageMemoryBarrier
    {
        private UInt32 sType = 45; // VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER
        private UIntPtr pNext = UIntPtr.Zero;
        public VkAccessFlags srcAccessMask;
        public VkAccessFlags dstAccessMask;
        public VkImageLayout oldLayout;
        public VkImageLayout newLayout;
        private UInt32 srcQueueFamilyIndex = 0;
        private UInt32 dstQueueFamilyIndex = 0;
        public ResourceId image;

        // inlined subresourceRange
        private VkImageAspectFlags subresourceRange_aspectMask = VkImageAspectFlags.COLOR_BIT;
        private UInt32 subresourceRange_baseMipLevel;
        private UInt32 subresourceRange_levelCount = VkImageSubresourceRange.VK_REMAINING_MIP_LEVELS;
        private UInt32 subresourceRange_baseArrayLayer;
        private UInt32 subresourceRange_layerCount = VkImageSubresourceRange.VK_REMAINING_ARRAY_LAYERS;

        public VkImageSubresourceRange subresourceRange
        {
            get
            {
                VkImageSubresourceRange ret = new VkImageSubresourceRange();
                ret.aspectMask = subresourceRange_aspectMask;
                ret.baseMipLevel = subresourceRange_baseMipLevel;
                ret.levelCount = subresourceRange_levelCount;
                ret.baseArrayLayer = subresourceRange_baseArrayLayer;
                ret.layerCount = subresourceRange_layerCount;
                return ret;
            }
            set
            {
                subresourceRange_aspectMask = value.aspectMask;
                subresourceRange_baseMipLevel = value.baseMipLevel;
                subresourceRange_levelCount = value.levelCount;
                subresourceRange_baseArrayLayer = value.baseArrayLayer;
                subresourceRange_layerCount = value.layerCount;
            }
        }

        public override string ToString()
        {
            return String.Format("VkMemoryBarrier[ src: {0} dst: {1} image: {2} range: {3} oldLayout: {4} newLayout: {5}]", srcAccessMask, dstAccessMask, image, subresourceRange, oldLayout, newLayout);
        }
    };

    ///////////////////////////// END HACK //////////////////////////////////

    public struct Vec3f
    {
        public Vec3f(float X, float Y, float Z) { x = X; y = Y; z = Z; }
        public Vec3f(Vec3f v) { x = v.x; y = v.y; z = v.z; }
        public Vec3f(FloatVector v) { x = v.x; y = v.y; z = v.z; }

        public float Length()
        {
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }

        public Vec3f Sub(Vec3f o)
        {
            return new Vec3f(x - o.x,
                                y - o.y,
                                z - o.z);
        }

        public void Mul(float f)
        {
            x *= f;
            y *= f;
            z *= f;
        }

        public float x, y, z;
    };

    public struct Vec4f
    {
        public float x, y, z, w;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct FloatVector
    {
        public FloatVector(float X, float Y, float Z, float W) { x = X; y = Y; z = Z; w = W; }
        public FloatVector(float X, float Y, float Z) { x = X; y = Y; z = Z; w = 1; }
        public FloatVector(Vec3f v) { x = v.x; y = v.y; z = v.z; w = 1; }
        public FloatVector(Vec3f v, float W) { x = v.x; y = v.y; z = v.z; w = W; }

        public float x, y, z, w;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct DirectoryFile : IComparable<DirectoryFile>
    {
        [CustomMarshalAs(CustomUnmanagedType.UTF8TemplatedString)]
        public string filename;
        public DirectoryFileProperty flags;

        public override string ToString()
        {
            return String.Format("{0} ({1})", filename, flags);
        }

        public int CompareTo(DirectoryFile o)
        {
            // sort directories first
            bool thisdir = flags.HasFlag(DirectoryFileProperty.Directory);
            bool odir = o.flags.HasFlag(DirectoryFileProperty.Directory);

            if (thisdir && !odir)
                return -1;
            if (!thisdir && odir)
                return 1;

            // can't have duplicates with same filename, so just compare filenames
            return filename.CompareTo(o.filename);
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public class ResourceFormat
    {
        [DllImport("renderdoc.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern float Maths_HalfToFloat(UInt16 half);

        public ResourceFormat()
        {
            rawType = 0;
            special = false;
            specialFormat = SpecialFormat.Unknown;

            compType = FormatComponentType.None;
            compCount = 0;
            compByteWidth = 0;
            bgraOrder = false;
            srgbCorrected = false;

            strname = "";
        }

        public ResourceFormat(FormatComponentType type, UInt32 count, UInt32 byteWidth)
        {
            rawType = 0;
            special = false;
            specialFormat = SpecialFormat.Unknown;

            compType = type;
            compCount = count;
            compByteWidth = byteWidth;
            bgraOrder = false;
            srgbCorrected = false;

            strname = "";
        }

        public UInt32 rawType;

        // indicates it's not a type represented with the members below
        // usually this means non-uniform across components or block compressed
        public bool special;
        public SpecialFormat specialFormat;

        [CustomMarshalAs(CustomUnmanagedType.UTF8TemplatedString)]
        public string strname;

        public UInt32 compCount;
        public UInt32 compByteWidth;
        public FormatComponentType compType;

        public bool bgraOrder;
        public bool srgbCorrected;

        public override string ToString()
        {
            return strname;
        }

        public override bool Equals(Object obj)
        {
            return obj is ResourceFormat && this == (ResourceFormat)obj;
        }
        public override int GetHashCode()
        {
            int hash = specialFormat.GetHashCode() * 17;
            hash = hash * 17 + compCount.GetHashCode();
            hash = hash * 17 + compByteWidth.GetHashCode();
            hash = hash * 17 + compType.GetHashCode();
            hash = hash * 17 + bgraOrder.GetHashCode();
            hash = hash * 17 + srgbCorrected.GetHashCode();
            return hash;
        }
        public static bool operator ==(ResourceFormat x, ResourceFormat y)
        {
            if ((object)x == null) return (object)y == null;
            if ((object)y == null) return (object)x == null;

            if (x.special || y.special)
                return x.special == y.special && x.specialFormat == y.specialFormat && x.compType == y.compType;

            return x.compCount == y.compCount &&
                x.compByteWidth == y.compByteWidth &&
                x.compType == y.compType &&
                x.bgraOrder == y.bgraOrder &&
                x.srgbCorrected == y.srgbCorrected;
        }
        public static bool operator !=(ResourceFormat x, ResourceFormat y)
        {
            return !(x == y);
        }

        public float ConvertFromHalf(UInt16 comp)
        {
            return Maths_HalfToFloat(comp);
        }

        public object Interpret(UInt16 comp)
        {
            if (compByteWidth != 2 || compType == FormatComponentType.Float) throw new ArgumentException();

            if (compType == FormatComponentType.SInt)
            {
                return (Int16)comp;
            }
            else if (compType == FormatComponentType.UInt)
            {
                return comp;
            }
            else if (compType == FormatComponentType.SScaled)
            {
                return (float)((Int16)comp);
            }
            else if (compType == FormatComponentType.UScaled)
            {
                return (float)comp;
            }
            else if (compType == FormatComponentType.UNorm)
            {
                return (float)comp / (float)UInt16.MaxValue;
            }
            else if (compType == FormatComponentType.SNorm)
            {
                Int16 cast = (Int16)comp;

                float f = -1.0f;

                if (cast == -32768)
                    f = -1.0f;
                else
                    f = ((float)cast) / 32767.0f;

                return f;
            }

            throw new ArgumentException();
        }

        public object Interpret(byte comp)
        {
            if (compByteWidth != 1 || compType == FormatComponentType.Float) throw new ArgumentException();

            if (compType == FormatComponentType.SInt)
            {
                return (sbyte)comp;
            }
            else if (compType == FormatComponentType.UInt)
            {
                return comp;
            }
            else if (compType == FormatComponentType.SScaled)
            {
                return (float)((sbyte)comp);
            }
            else if (compType == FormatComponentType.UScaled)
            {
                return (float)comp;
            }
            else if (compType == FormatComponentType.UNorm)
            {
                return ((float)comp) / 255.0f;
            }
            else if (compType == FormatComponentType.SNorm)
            {
                sbyte cast = (sbyte)comp;

                float f = -1.0f;

                if (cast == -128)
                    f = -1.0f;
                else
                    f = ((float)cast) / 127.0f;

                return f;
            }

            throw new ArgumentException();
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchBuffer
    {
        public ResourceId ID;
        [CustomMarshalAs(CustomUnmanagedType.UTF8TemplatedString)]
        public string name;
        public bool customName;
        public BufferCreationFlags creationFlags;
        public UInt64 length;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchTexture
    {
        [CustomMarshalAs(CustomUnmanagedType.UTF8TemplatedString)]
        public string name;
        public bool customName;
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public ResourceFormat format;
        public UInt32 dimension;
        public ShaderResourceType resType;
        public UInt32 width, height, depth;
        public ResourceId ID;
        public bool cubemap;
        public UInt32 mips;
        public UInt32 arraysize;
        public TextureCreationFlags creationFlags;
        public UInt32 msQual, msSamp;
        public UInt64 byteSize;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class OutputConfig
    {
        public OutputType m_Type = OutputType.None;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameConstantBindStats
    {
        public UInt32 calls;
        public UInt32 sets;
        public UInt32 nulls;
        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public UInt32[] bindslots;
        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public UInt32[] sizes;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameSamplerBindStats
    {
        public UInt32 calls;
        public UInt32 sets;
        public UInt32 nulls;
        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public UInt32[] bindslots;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameResourceBindStats
    {
        public UInt32 calls;
        public UInt32 sets;
        public UInt32 nulls;
        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public UInt32[] types;
        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public UInt32[] bindslots;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameUpdateStats
    {
        public UInt32 calls;
        public UInt32 clients;
        public UInt32 servers;
        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public UInt32[] types;
        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public UInt32[] sizes;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameDrawStats
    {
        public UInt32 calls;
        public UInt32 instanced;
        public UInt32 indirect;
        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public UInt32[] counts;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameDispatchStats
    {
        public UInt32 calls;
        public UInt32 indirect;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameIndexBindStats
    {
        public UInt32 calls;
        public UInt32 sets;
        public UInt32 nulls;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameVertexBindStats
    {
        public UInt32 calls;
        public UInt32 sets;
        public UInt32 nulls;
        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public UInt32[] bindslots;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameLayoutBindStats
    {
        public UInt32 calls;
        public UInt32 sets;
        public UInt32 nulls;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameShaderStats
    {
        public UInt32 calls;
        public UInt32 sets;
        public UInt32 nulls;
        public UInt32 redundants;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameBlendStats
    {
        public UInt32 calls;
        public UInt32 sets;
        public UInt32 nulls;
        public UInt32 redundants;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameDepthStencilStats
    {
        public UInt32 calls;
        public UInt32 sets;
        public UInt32 nulls;
        public UInt32 redundants;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameRasterizationStats
    {
        public UInt32 calls;
        public UInt32 sets;
        public UInt32 nulls;
        public UInt32 redundants;
        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public UInt32[] viewports;
        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public UInt32[] rects;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameOutputStats
    {
        public UInt32 calls;
        public UInt32 sets;
        public UInt32 nulls;
        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public UInt32[] bindslots;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchFrameStatistics
    {
        public UInt32 recorded;
        [CustomMarshalAs(CustomUnmanagedType.FixedArray, FixedLength = (int)ShaderStageType.Count)]
        public FetchFrameConstantBindStats[] constants;
        [CustomMarshalAs(CustomUnmanagedType.FixedArray, FixedLength = (int)ShaderStageType.Count)]
        public FetchFrameSamplerBindStats[] samplers;
        [CustomMarshalAs(CustomUnmanagedType.FixedArray, FixedLength = (int)ShaderStageType.Count)]
        public FetchFrameResourceBindStats[] resources;
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public FetchFrameUpdateStats updates;
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public FetchFrameDrawStats draws;
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public FetchFrameDispatchStats dispatches;
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public FetchFrameIndexBindStats indices;
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public FetchFrameVertexBindStats vertices;
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public FetchFrameLayoutBindStats layouts;
        [CustomMarshalAs(CustomUnmanagedType.FixedArray, FixedLength = (int)ShaderStageType.Count)]
        public FetchFrameShaderStats[] shaders;
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public FetchFrameBlendStats blends;
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public FetchFrameDepthStencilStats depths;
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public FetchFrameRasterizationStats rasters;
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public FetchFrameOutputStats outputs;
    };

[StructLayout(LayoutKind.Sequential)]
    public class FetchFrameInfo
    {
        public UInt32 frameNumber;
        public UInt32 firstEvent;
        public UInt64 fileOffset;
        public UInt64 uncompressedFileSize;
        public UInt64 compressedFileSize;
        public UInt64 persistentSize;
        public UInt64 initDataSize;
        public UInt64 captureTime;
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public FetchFrameStatistics stats;

        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public DebugMessage[] debugMessages;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchAPIEvent
    {
        public UInt32 eventID;

        public ResourceId context;

        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public UInt64[] callstack;

        [CustomMarshalAs(CustomUnmanagedType.UTF8TemplatedString)]
        public string eventDesc;

        public UInt64 fileOffset;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class DebugMessage
    {
        public UInt32 eventID;
        public DebugMessageCategory category;
        public DebugMessageSeverity severity;
        public DebugMessageSource source;
        public UInt32 messageID;
        [CustomMarshalAs(CustomUnmanagedType.UTF8TemplatedString)]
        public string description;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class EventUsage
    {
        public UInt32 eventID;
        public ResourceUsage usage;
        public ResourceId view;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class FetchDrawcall
    {
        public UInt32 eventID, drawcallID;

        [CustomMarshalAs(CustomUnmanagedType.UTF8TemplatedString)]
        public string name;

        public DrawcallFlags flags;
        
        [CustomMarshalAs(CustomUnmanagedType.FixedArray, FixedLength = 4)]
        public float[] markerColour;

        public System.Drawing.Color GetColor()
        {
            float red = markerColour[0];
            float green = markerColour[1];
            float blue = markerColour[2];
            float alpha = markerColour[3];

            return System.Drawing.Color.FromArgb(
                (int)(alpha * 255.0f),
                (int)(red * 255.0f),
                (int)(green * 255.0f),
                (int)(blue * 255.0f)
                );
        }

        public System.Drawing.Color GetTextColor(System.Drawing.Color defaultTextCol)
        {
            float backLum = GetColor().GetLuminance();
            float textLum = defaultTextCol.GetLuminance();

            bool backDark = backLum < 0.2f;
            bool textDark = textLum < 0.2f;

            // if they're contrasting, use the text colour desired
            if (backDark != textDark)
                return defaultTextCol;

            // otherwise pick a contrasting colour
            if (backDark)
                return System.Drawing.Color.White;
            else
                return System.Drawing.Color.Black;
        }

        public UInt32 numIndices;
        public UInt32 numInstances;
        public Int32 baseVertex;
        public UInt32 indexOffset;
        public UInt32 vertexOffset;
        public UInt32 instanceOffset;

        [CustomMarshalAs(CustomUnmanagedType.FixedArray, FixedLength = 3)]
        public UInt32[] dispatchDimension;
        [CustomMarshalAs(CustomUnmanagedType.FixedArray, FixedLength = 3)]
        public UInt32[] dispatchThreadsDimension;

        public UInt32 indexByteWidth;
        public PrimitiveTopology topology;

        public ResourceId copySource;
        public ResourceId copyDestination;

        public Int64 parentDrawcall;
        public Int64 previousDrawcall;
        public Int64 nextDrawcall;

        [CustomMarshalAs(CustomUnmanagedType.Skip)]
        public FetchDrawcall parent;
        [CustomMarshalAs(CustomUnmanagedType.Skip)]
        public FetchDrawcall previous;
        [CustomMarshalAs(CustomUnmanagedType.Skip)]
        public FetchDrawcall next;

        [CustomMarshalAs(CustomUnmanagedType.FixedArray, FixedLength = 8)]
        public ResourceId[] outputs;
        public ResourceId depthOut;

        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public FetchAPIEvent[] events;
        [CustomMarshalAs(CustomUnmanagedType.TemplatedArray)]
        public FetchDrawcall[] children;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct MeshFormat
    {
        public ResourceId idxbuf;
        public UInt64 idxoffs;
        public UInt32 idxByteWidth;
        public Int32 baseVertex;

        public ResourceId buf;
        public UInt64 offset;
        public UInt32 stride;

        public UInt32 compCount;
        public UInt32 compByteWidth;
        public FormatComponentType compType;
        public bool bgraOrder;
        public SpecialFormat specialFormat;

        public FloatVector meshColour;

        public bool showAlpha;

        public PrimitiveTopology topo;
        public UInt32 numVerts;

        public bool unproject;
        public float nearPlane;
        public float farPlane;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class MeshDisplay
    {
        public MeshDataStage type = MeshDataStage.Unknown;

        public IntPtr cam = IntPtr.Zero;

        public bool ortho = false;
        public float fov = 90.0f;
        public float aspect = 0.0f;

        public bool showPrevInstances = false;
        public bool showAllInstances = false;
        public bool showWholePass = false;
        public UInt32 curInstance = 0;

        public UInt32 highlightVert;
        public MeshFormat position;
        public MeshFormat secondary;

        public FloatVector minBounds = new FloatVector();
        public FloatVector maxBounds = new FloatVector();
        public bool showBBox = false;

        public SolidShadeMode solidShadeMode = SolidShadeMode.None;
        public bool wireframeDraw = true;
    };
    
    [StructLayout(LayoutKind.Sequential)]
    public class TextureDisplay
    {
        public ResourceId texid = ResourceId.Null;
        public FormatComponentType typeHint = FormatComponentType.None;
        public float rangemin = 0.0f;
        public float rangemax = 1.0f;
        public float scale = 1.0f;
        public bool Red = true, Green = true, Blue = true, Alpha = false;
        public bool FlipY = false;
        public float HDRMul = -1.0f;
        public bool linearDisplayAsGamma = true;
        public ResourceId CustomShader = ResourceId.Null;
        public UInt32 mip = 0;
        public UInt32 sliceFace = 0;
        public UInt32 sampleIdx = 0;
        public bool rawoutput = false;

        public float offx = 0.0f, offy = 0.0f;

        public FloatVector lightBackgroundColour = new FloatVector(0.81f, 0.81f, 0.81f);
        public FloatVector darkBackgroundColour = new FloatVector(0.57f, 0.57f, 0.57f);

        public TextureDisplayOverlay overlay = TextureDisplayOverlay.None;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class TextureSave
    {
        public ResourceId id = ResourceId.Null;

        public FormatComponentType typeHint = FormatComponentType.None;

        public FileType destType = FileType.DDS;

        public Int32 mip = -1;

        [StructLayout(LayoutKind.Sequential)]
        public class ComponentMapping
        {
            public float blackPoint;
            public float whitePoint;
        };

        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public ComponentMapping comp = new ComponentMapping();

        [StructLayout(LayoutKind.Sequential)]
        public class SampleMapping
        {
            public bool mapToArray;

            public UInt32 sampleIndex;
        };
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public SampleMapping sample = new SampleMapping();

        [StructLayout(LayoutKind.Sequential)]
        public class SliceMapping
        {
            public Int32 sliceIndex;

            public bool slicesAsGrid;

            public Int32 sliceGridWidth;

            public bool cubeCruciform;
        };
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public SliceMapping slice = new SliceMapping();

        public int channelExtract = -1;

        public AlphaMapping alpha = AlphaMapping.Discard;
        public FloatVector alphaCol = new FloatVector(0.81f, 0.81f, 0.81f);
        public FloatVector alphaColSecondary = new FloatVector(0.57f, 0.57f, 0.57f);

        public int jpegQuality = 90;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class APIProperties
    {
        public GraphicsAPI pipelineType;
        public GraphicsAPI localRenderer;
        public bool degraded;

        public string ShaderExtension
        {
            get
            {
                return pipelineType.IsD3D() ? ".hlsl" : ".glsl";
            }
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public class CounterDescription
    {
        public UInt32 counterID;
        [CustomMarshalAs(CustomUnmanagedType.UTF8TemplatedString)]
        public string name;
        [CustomMarshalAs(CustomUnmanagedType.UTF8TemplatedString)]
        public string description;
        public FormatComponentType resultCompType;
        public UInt32 resultByteWidth;
        public CounterUnits units;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class CounterResult
    {
        public UInt32 eventID;
        public UInt32 counterID;

        [StructLayout(LayoutKind.Sequential)]
        public struct ValueUnion
        {
            public float f;
            public double d;
            public UInt32 u32;
            public UInt64 u64;
        };

        [CustomMarshalAs(CustomUnmanagedType.Union)]
        public ValueUnion value;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class PixelValue
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct ValueUnion
        {
            [CustomMarshalAs(CustomUnmanagedType.FixedArray, FixedLength = 4, FixedType = CustomFixedType.UInt32)]
            public UInt32[] u;

            [CustomMarshalAs(CustomUnmanagedType.FixedArray, FixedLength = 4, FixedType = CustomFixedType.Float)]
            public float[] f;

            [CustomMarshalAs(CustomUnmanagedType.FixedArray, FixedLength = 4, FixedType = CustomFixedType.Int32)]
            public Int32[] i;

            [CustomMarshalAs(CustomUnmanagedType.FixedArray, FixedLength = 4, FixedType = CustomFixedType.UInt16)]
            public UInt16[] u16;
        };

        [CustomMarshalAs(CustomUnmanagedType.Union)]
        public ValueUnion value;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class ModificationValue
    {
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public PixelValue col;
        public float depth;
        public Int32 stencil;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class PixelModification
    {
        public UInt32 eventID;

        public bool uavWrite;
        public bool unboundPS;

        public UInt32 fragIndex;
        public UInt32 primitiveID;

        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public ModificationValue preMod;
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public ModificationValue shaderOut;
        [CustomMarshalAs(CustomUnmanagedType.CustomClass)]
        public ModificationValue postMod;

        public bool sampleMasked;
        public bool backfaceCulled;
        public bool depthClipped;
        public bool viewClipped;
        public bool scissorClipped;
        public bool shaderDiscarded;
        public bool depthTestFailed;
        public bool stencilTestFailed;

        public bool EventPassed()
        {
            return !sampleMasked && !backfaceCulled && !depthClipped &&
                !viewClipped && !scissorClipped && !shaderDiscarded &&
                !depthTestFailed && !stencilTestFailed;
        }
    };
}
