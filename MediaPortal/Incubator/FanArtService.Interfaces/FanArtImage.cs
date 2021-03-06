#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.Graphics;

namespace MediaPortal.Extensions.UserServices.FanArtService.Interfaces
{
  /// <summary>
  /// <see cref="FanArtImage"/> represents a fanart image that can be transmitted from server to clients using UPnP.
  /// It supports resizing of existing images to different resolutions. The resized images will be cached on server data folder
  /// (<see cref="CACHE_PATH"/>), so they can be reused later.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class FanArtImage
  {
    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\Thumbs\FanArt");

    // We could use some cache for this instance, if we would have one...
    protected static XmlSerializer _xmlSerializer; // Lazy initialized

    public FanArtImage()
    { }

    public FanArtImage(string name, byte[] binaryData)
    {
      Name = name;
      BinaryData = binaryData;
    }

    /// <summary>
    /// Returns the name of this FanArtImage.
    /// </summary>
    [XmlAttribute("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Contains the binary data of the Image.
    /// </summary>
    [XmlElement("BinaryData")]
    public byte[] BinaryData { get; set; }

    /// <summary>
    /// Serializes this user profile instance to XML.
    /// </summary>
    /// <returns>String containing an XML fragment with this instance's data.</returns>
    public string Serialize()
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      StringBuilder sb = new StringBuilder(); // Will contain the data, formatted as XML
      using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true }))
        xs.Serialize(writer, this);
      return sb.ToString();
    }

    /// <summary>
    /// Serializes this user profile instance to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Writer to write the XML serialization to.</param>
    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      xs.Serialize(writer, this);
    }

    /// <summary>
    /// Deserializes a user profile instance from a given XML fragment.
    /// </summary>
    /// <param name="str">XML fragment containing a serialized user profile instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static FanArtImage Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      using (StringReader reader = new StringReader(str))
        return xs.Deserialize(reader) as FanArtImage;
    }

    /// <summary>
    /// Deserializes a user profile instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a serialized user profile instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static FanArtImage Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as FanArtImage;
    }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(FanArtImage)));
    }

    /// <summary>
    /// Loads an image from filesystem an returns a new <see cref="FanArtImage"/>.
    /// </summary>
    /// <param name="resourceLocator">Resource to load</param>
    /// <param name="maxWidth">Maximum width for image. <c>0</c> returns image in original size.</param>
    /// <param name="maxHeight">Maximum height for image. <c>0</c> returns image in original size.</param>
    /// <param name="mediaType">MediaType</param>
    /// <param name="fanArtType">FanArtType</param>
    /// <param name="fanArtName">Fanart name</param>
    /// <returns>FanArtImage or <c>null</c>.</returns>
    public static FanArtImage FromResource(IResourceLocator resourceLocator, int maxWidth, int maxHeight, FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string fanArtName)
    {
      try
      {
        using (var ra = resourceLocator.CreateAccessor())
        {
          ILocalFsResourceAccessor fsra = ra as ILocalFsResourceAccessor;
          if (fsra != null)
          {
            fsra.PrepareStreamAccess();
            using (var fileStream = fsra.OpenRead())
              return FromStream(fileStream, maxWidth, maxHeight, mediaType, fanArtType, fanArtName, fsra.LocalFileSystemPath);
          }
        }
      }
      catch
      {
      }
      return null;
    }

    public static FanArtImage FromStream(Stream stream, int maxWidth, int maxHeight, FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string fanArtName, string fileName = null)
    {
      using (Stream resized = ResizeImage(stream, maxWidth, maxHeight, mediaType, fanArtType, fanArtName, fileName))
        return new FanArtImage(fileName, ReadAll(resized));
    }

    public static byte[] ReadAll(Stream stream)
    {
      stream.Position = 0;
      using (BinaryReader binaryReader = new BinaryReader(stream))
      {
        byte[] binary = new byte[stream.Length];
        binaryReader.Read(binary, 0, binary.Length);
        return binary;
      }
    }

    /// <summary>
    /// Resizes an image to given size. The resized image will be saved to the given stream. Images that
    /// are smaller than the requested target size will not be scaled up, but returned in original size.
    /// </summary>
    /// <param name="originalStream">Image to resize</param>
    /// <param name="maxWidth">Maximum image width</param>
    /// <param name="maxHeight">Maximum image height</param>
    /// <param name="mediaType">MediaType</param>
    /// <param name="fanArtType">FanArtType</param>
    /// <param name="fanArtName">Fanart name</param>
    /// <param name="originalFile">Original Filename</param>
    /// <returns></returns>
    protected static Stream ResizeImage(Stream originalStream, int maxWidth, int maxHeight, FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string fanArtName, string originalFile)
    {
      if (maxWidth == 0 || maxHeight == 0)
        return originalStream;

      try
      {
        if (!Directory.Exists(CACHE_PATH))
          Directory.CreateDirectory(CACHE_PATH);

        string thumbFileName = Path.Combine(CACHE_PATH, string.Format("{0}_{1}_{2}x{3}_{4}", fanArtType, fanArtName, maxWidth, maxHeight, Path.GetFileName(originalFile)));
        if (File.Exists(thumbFileName))
          using (originalStream)
            return new FileStream(thumbFileName, FileMode.Open, FileAccess.Read);

        Image fullsizeImage = Image.FromStream(originalStream);
        if (fullsizeImage.Width <= maxWidth)
          maxWidth = fullsizeImage.Width;

        int newHeight = fullsizeImage.Height * maxWidth / fullsizeImage.Width;
        if (newHeight > maxHeight)
        {
          // Resize with height instead
          maxWidth = fullsizeImage.Width * maxHeight / fullsizeImage.Height;
          newHeight = maxHeight;
        }

        MemoryStream resizedStream = new MemoryStream();
        using (fullsizeImage)
        using (Image newImage = ImageUtilities.ResizeImage(fullsizeImage, maxWidth, newHeight))
        {
          ImageUtilities.SaveJpeg(thumbFileName, newImage, 95);
          ImageUtilities.SaveJpeg(resizedStream, newImage, 95);
        }

        resizedStream.Position = 0;
        return resizedStream;
      }
      catch (Exception)
      {
        return originalStream;
      }
    }
  }
}
