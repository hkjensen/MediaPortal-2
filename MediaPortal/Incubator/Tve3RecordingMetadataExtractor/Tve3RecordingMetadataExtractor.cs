#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.Aspects;

namespace MediaPortal.Extensions.MetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor for TVE3 recordings.
  /// </summary>
  public class Tve3RecordingMetadataExtractor : IMetadataExtractor
  {
    #region Helper classes for simple XML deserialization

    [XmlRoot(ElementName = "SimpleTag")]
    public class SimpleTag
    {
      [XmlElement(ElementName = "name")]
      public string Name { get; set; }
      [XmlElement(ElementName = "value")]
      public string Value { get; set; }
    }


    [XmlRoot(ElementName = "tags")]
    public class Tags
    {
      [XmlArray(ElementName = "tag")]
      public List<SimpleTag> Tag;
    }

    #endregion

    #region Public constants

    /// <summary>
    /// GUID string for the Tve3Recording metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "C7080745-8EAE-459E-8A9A-25D87DF8565F";

    /// <summary>
    /// Video metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    const string TAG_TITLE = "TITLE";
    const string TAG_PLOT = "COMMENT";
    const string TAG_GENRE = "GENRE";
    const string TAG_CHANNEL = "CHANNEL_NAME";
    const string TAG_EPISODENAME = "EPISODENAME";
    const string TAG_SERIESNUM = "SERIESNUM";
    const string TAG_EPISODENUM = "EPISODENUM";
    const string TAG_EPISODEPART = "EPISODEPART";
    const string TAG_STARTTIME = "STARTTIME";
    const string TAG_ENDTIME = "ENDTIME";

    #endregion

    #region Protected fields and classes

    protected static IList<string> SHARE_CATEGORIES = new List<string>();
    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static Tve3RecordingMetadataExtractor()
    {
      SHARE_CATEGORIES.Add(DefaultMediaCategory.Video.ToString());
      // Register the Recording aspect
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(RecordingAspect.Metadata);
    }

    public Tve3RecordingMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "TVEngine3 recordings metadata extractor", true,
          SHARE_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                VideoAspect.Metadata,
                RecordingAspect.Metadata
              });
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;
        if (fsra == null || !mediaItemAccessor.IsFile)
          return false;

        string filePath = mediaItemAccessor.ResourcePathName;
        string metaFile = Path.ChangeExtension(filePath, ".xml");
        // FIXME: how to access sibling file best way?
        if (!filePath.EndsWith(".ts") || !File.Exists(metaFile))
          return false;

        MediaItemAspect mediaAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, MediaAspect.ASPECT_ID, MediaAspect.Metadata);
        MediaItemAspect videoAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, VideoAspect.ASPECT_ID, VideoAspect.Metadata);
        MediaItemAspect recordingAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, RecordingAspect.ASPECT_ID, RecordingAspect.Metadata);

        Tags tags;
        XmlSerializer serializer = new XmlSerializer(typeof(Tags));
        using (FileStream fileStream = new FileStream(metaFile, FileMode.Open))
          tags = (Tags) serializer.Deserialize(fileStream);

        string title;
        if (TryGet(tags, TAG_TITLE, out title))
        {
          string episodeName;
          string seasonNum;
          string episodeNum = null;
          string formattedTitle = title;

          if (TryGet(tags, TAG_SERIESNUM, out seasonNum) && TryGet(tags, TAG_EPISODENUM, out episodeNum))
            formattedTitle = string.Format("{0} - S{1}E{2}", formattedTitle, seasonNum, episodeNum);

          if (TryGet(tags, TAG_EPISODENAME, out episodeName))
            formattedTitle = string.Format("{0} - {1}", formattedTitle, episodeName);

          mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, formattedTitle);

          int seasonNumber;
          if (int.TryParse(seasonNum, out seasonNumber))
            MediaItemAspect.SetAttribute(_metadata, extractedAspectData, SeriesAspect.ASPECT_ID, SeriesAspect.Metadata, SeriesAspect.ATTR_SEASONNUMBER, seasonNumber);

          // TODO: how does Tve3/EPG/EpisodeScanner store combined episodes?
          int episodeNumber;
          if (int.TryParse(episodeNum, out episodeNumber))
            MediaItemAspect.SetCollectionAttribute(_metadata, extractedAspectData, SeriesAspect.ASPECT_ID, SeriesAspect.Metadata, SeriesAspect.ATTR_EPISODENUMBER, new List<int> { episodeNumber });

          // Only fill the series name if there is also an episode. Otherwise the normal media item title will be enough.
          // This check needs to be done, because the EpisodeName tag is often filled with the original title (i.e. by TvMovie EPG importer)
          if (!string.IsNullOrEmpty(episodeName) && episodeNumber > 0)
          {
            MediaItemAspect.SetAttribute(_metadata, extractedAspectData, SeriesAspect.ASPECT_ID, SeriesAspect.Metadata, SeriesAspect.ATTR_SERIESNAME, title);
            MediaItemAspect.SetAttribute(_metadata, extractedAspectData, SeriesAspect.ASPECT_ID, SeriesAspect.Metadata, SeriesAspect.ATTR_EPISODENAME, episodeName);
          }
        }

        string value;
        if (TryGet(tags, TAG_GENRE, out value))
          videoAspect.SetCollectionAttribute(VideoAspect.ATTR_GENRES, new List<String> { value });

        if (TryGet(tags, TAG_PLOT, out value))
          videoAspect.SetAttribute(VideoAspect.ATTR_STORYPLOT, value);

        if (TryGet(tags, TAG_CHANNEL, out value))
          recordingAspect.SetAttribute(RecordingAspect.ATTR_CHANNEL, value);

        // Recording date formatted: 2011-11-04 20:55
        DateTime recordingStart;
        DateTime recordingEnd;
        if (TryGet(tags, TAG_STARTTIME, out value) && DateTime.TryParse(value, out recordingStart))
        {
          mediaAspect.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, recordingStart);
          recordingAspect.SetAttribute(RecordingAspect.ATTR_STARTTIME, recordingStart);
        }
        if (TryGet(tags, TAG_ENDTIME, out value) && DateTime.TryParse(value, out recordingEnd))
          recordingAspect.SetAttribute(RecordingAspect.ATTR_ENDTIME, recordingEnd);

        return true;
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("Tve3RecordingMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    private static bool TryGet(Tags tags, string key, out string value)
    {
      value = null;
      SimpleTag tag = tags.Tag.Find(t => t.Name == key);
      if (tag == null || tag.Value == null)
        return false;

      value = tag.Value.Trim();
      return !string.IsNullOrEmpty(value);
    }

    #endregion
  }
}