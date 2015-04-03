﻿#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  /// <summary>
  /// Reads the content of a nfo-file for movies into <see cref="MovieStub"/> object and stores
  /// the appropriate values into the respective <see cref="MediaItemAspect"/>s
  /// </summary>
  /// <remarks>
  /// There is a TryRead method for any known child element of the nfo-file's root element.
  /// ToDo: When implementing the NfoSeriesMetadataExtractor we need to move all TryRead methods that can be used
  /// ToDo: by both classes to a common base class (e.g. NfoVideoReaderBase) that derives from NfoReaderBase
  /// </remarks>
  class NfoMovieReader : NfoReaderBase<MovieStub>
  {
    #region Consts

    /// <summary>
    /// The name of the root element in a valid nfo-file for movies
    /// </summary>
    private const string MOVIE_ROOT_ELEMENT_NAME = "movie";

    #endregion

    #region Ctor

    /// <summary>
    /// Instantiates a <see cref="NfoMovieReader"/> object
    /// </summary>
    /// <param name="debugLogger">Debug logger to log to</param>
    /// <param name="miNumber">Unique number of the MediaItem for which the nfo-file is parsed</param>
    /// <param name="forceQuickMode">If true, no long lasting operations such as parsing images are performed</param>
    /// <param name="httpClient"><see cref="HttpClient"/> used to download from http URLs contained in nfo-files</param>
    /// <param name="settings">Settings of the <see cref="NfoMovieMetadataExtractor"/></param>
    public NfoMovieReader(ILogger debugLogger, long miNumber, bool forceQuickMode, HttpClient httpClient, NfoMovieMetadataExtractorSettings settings)
      : base(debugLogger, miNumber, forceQuickMode, httpClient, settings)
    {
      InitializeSupportedElements();
      InitializeSupportedAttributes();
    }

    #endregion

    #region Private methods

    #region Ctor helpers

    /// <summary>
    /// Adds a delegate for each xml element in a movie nfo-file that is understood by this MetadataExtractor to NfoReaderBase.SupportedElements
    /// </summary>
    private void InitializeSupportedElements()
    {
      SupportedElements.Add("id", new TryReadElementDelegate(TryReadId));
      SupportedElements.Add("imdb", new TryReadElementDelegate(TryReadImdb));
      SupportedElements.Add("tmdbid", new TryReadElementDelegate(TryReadTmdbId));
      SupportedElements.Add("tmdbId", new TryReadElementDelegate(TryReadTmdbId)); // Tiny Media Manager (v2.6.5) uses <tmdbId> instead of <tmdbid>
      SupportedElements.Add("thmdb", new TryReadElementDelegate(TryReadThmdb));
      SupportedElements.Add("ids", new TryReadElementDelegate(TryReadIds)); // Used by Tiny MediaManager (as of v2.6.0)
      SupportedElements.Add("allocine", new TryReadElementDelegate(TryReadAllocine));
      SupportedElements.Add("cinepassion", new TryReadElementDelegate(TryReadCinepassion));
      SupportedElements.Add("title", new TryReadElementDelegate(TryReadTitle));
      SupportedElements.Add("originaltitle", new TryReadElementDelegate(TryReadOriginalTitle));
      SupportedElements.Add("sorttitle", new TryReadElementDelegate(TryReadSortTitle));
      SupportedElements.Add("set", new TryReadElementAsyncDelegate(TryReadSetAsync));
      SupportedElements.Add("sets", new TryReadElementAsyncDelegate(TryReadSetsAsync));
      SupportedElements.Add("premiered", new TryReadElementDelegate(TryReadPremiered));
      SupportedElements.Add("year", new TryReadElementDelegate(TryReadYear));
      SupportedElements.Add("country", new TryReadElementDelegate(TryReadCountry));
      SupportedElements.Add("company", new TryReadElementDelegate(TryReadCompany));
      SupportedElements.Add("studio", new TryReadElementDelegate(TryReadStudio));
      SupportedElements.Add("studios", new TryReadElementDelegate(TryReadStudio)); // Synonym for <studio>
      SupportedElements.Add("actor", new TryReadElementAsyncDelegate(TryReadActorAsync));
      SupportedElements.Add("producer", new TryReadElementAsyncDelegate(TryReadProducerAsync));
      SupportedElements.Add("director", new TryReadElementDelegate(TryReadDirector));
      SupportedElements.Add("directorimdb", new TryReadElementDelegate(TryReadDirectorImdb));
      SupportedElements.Add("credits", new TryReadElementDelegate(TryReadCredits));
      SupportedElements.Add("plot", new TryReadElementDelegate(TryReadPlot));
      SupportedElements.Add("outline", new TryReadElementDelegate(TryReadOutline));
      SupportedElements.Add("tagline", new TryReadElementDelegate(TryReadTagline));
      SupportedElements.Add("trailer", new TryReadElementDelegate(TryReadTrailer));
      SupportedElements.Add("genre", new TryReadElementDelegate(TryReadGenre));
      SupportedElements.Add("genres", new TryReadElementDelegate(TryReadGenres));
      SupportedElements.Add("language", new TryReadElementDelegate(TryReadLanguage));
      SupportedElements.Add("languages", new TryReadElementDelegate(TryReadLanguage)); // Synonym for <language>
      SupportedElements.Add("thumb", new TryReadElementAsyncDelegate(TryReadThumbAsync));
      SupportedElements.Add("fanart", new TryReadElementAsyncDelegate(TryReadFanArtAsync));
      SupportedElements.Add("discart", new TryReadElementAsyncDelegate(TryReadDiscArtAsync));
      SupportedElements.Add("logo", new TryReadElementAsyncDelegate(TryReadLogoAsync));
      SupportedElements.Add("clearart", new TryReadElementAsyncDelegate(TryReadClearArtAsync));
      SupportedElements.Add("banner", new TryReadElementAsyncDelegate(TryReadBannerAsync));
      SupportedElements.Add("certification", new TryReadElementDelegate(TryReadCertification));
      SupportedElements.Add("mpaa", new TryReadElementDelegate(TryReadMpaa));
      SupportedElements.Add("rating", new TryReadElementDelegate(TryReadRating));
      SupportedElements.Add("ratings", new TryReadElementDelegate(TryReadRatings));
      SupportedElements.Add("votes", new TryReadElementDelegate(TryReadVotes));
      SupportedElements.Add("review", new TryReadElementDelegate(TryReadReview));
      SupportedElements.Add("top250", new TryReadElementDelegate(TryReadTop250));
      SupportedElements.Add("runtime", new TryReadElementDelegate(TryReadRuntime));
      SupportedElements.Add("fps", new TryReadElementDelegate(TryReadFps));
      SupportedElements.Add("rip", new TryReadElementDelegate(TryReadRip));
      SupportedElements.Add("fileinfo", new TryReadElementDelegate(TryReadFileInfo));
      SupportedElements.Add("epbookmark", new TryReadElementDelegate(TryReadEpBookmark));
      SupportedElements.Add("watched", new TryReadElementDelegate(TryReadWatched));
      SupportedElements.Add("playcount", new TryReadElementDelegate(TryReadPlayCount));
      SupportedElements.Add("lastplayed", new TryReadElementDelegate(TryReadLastPlayed));
      SupportedElements.Add("dateadded", new TryReadElementDelegate(TryReadDateAdded));
      SupportedElements.Add("resume", new TryReadElementDelegate(TryReadResume));

      // The following elements are contained in many movie.nfo files, but have no meaning
      // in the context of a movie. We add them here to avoid them being logged as
      // unknown elements, but we simply ignore them.
      // For reference see here: http://forum.kodi.tv/showthread.php?tid=40422&pid=244349#pid244349
      SupportedElements.Add("status", new TryReadElementDelegate(Ignore));
      SupportedElements.Add("code", new TryReadElementDelegate(Ignore));
      SupportedElements.Add("aired", new TryReadElementDelegate(Ignore));

      // We never need the following element as we get the same information in a more accurate way
      // from the filesystem itself; hence, we ignore it.
      SupportedElements.Add("filenameandpath", new TryReadElementDelegate(Ignore));
    }

    /// <summary>
    /// Adds a delegate for each Attribute in a MediaItemAspect into which this MetadataExtractor can write metadata to NfoReaderBase.SupportedAttributes
    /// </summary>
    private void InitializeSupportedAttributes()
    {
      SupportedAttributes.Add(TryWriteMediaAspectTitle);
      SupportedAttributes.Add(TryWriteMediaAspectRecordingTime);
      SupportedAttributes.Add(TryWriteMediaAspectPlayCount);
      SupportedAttributes.Add(TryWriteMediaAspectLastPlayed);

      SupportedAttributes.Add(TryWriteVideoAspectGenres);
      SupportedAttributes.Add(TryWriteVideoAspectActors);
      SupportedAttributes.Add(TryWriteVideoAspectDirectors);
      SupportedAttributes.Add(TryWriteVideoAspectStoryPlot);

      SupportedAttributes.Add(TryWriteMovieAspectMovieName);
      SupportedAttributes.Add(TryWriteMovieAspectOrigName);
      SupportedAttributes.Add(TryWriteMovieAspectTmdbId);
      SupportedAttributes.Add(TryWriteMovieAspectImdbId);
      SupportedAttributes.Add(TryWriteMovieAspectCollectionName);
      SupportedAttributes.Add(TryWriteMovieAspectRuntime);
      SupportedAttributes.Add(TryWriteMovieAspectCertification);
      SupportedAttributes.Add(TryWriteMovieAspectTagline);
      SupportedAttributes.Add(TryWriteMovieAspectTotalRating);
      SupportedAttributes.Add(TryWriteMovieAspectRatingCount);

      SupportedAttributes.Add(TryWriteThumbnailLargeAspectThumbnail);
    }

    #endregion

    #region Reader methods for direct child elements of the root element

    #region Internet databases

    /// <summary>
    /// Tries to read the Imdb ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadId(XElement element)
    {
      // Example of a valid element:
      // <id>tt0111161</id>
      return ((CurrentStub.Id = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the Imdb ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadImdb(XElement element)
    {
      // Example of a valid element:
      // <imdb>tt0810913</imdb>
      return ((CurrentStub.Imdb = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the Tmdb ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadTmdbId(XElement element)
    {
      // Examples of valid elements:
      // <tmdbId>52913<tmdbId>
      // <tmdbid>52913<tmdbid>
      return ((CurrentStub.TmdbId = ParseSimpleInt(element)) != null);
    }

    /// <summary>
    /// Tries to read the Tmdb ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadThmdb(XElement element)
    {
      // Example of a valid element:
      // <thmdb>52913</thmdb>
      return ((CurrentStub.Thmdb = ParseSimpleInt(element)) != null);
    }

    /// <summary>
    /// Tries to read the Allocine ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadAllocine(XElement element)
    {
      // Example of a valid element:
      // <allocine>52719</allocine>
      return ((CurrentStub.Allocine = ParseSimpleInt(element)) != null);
    }

    /// <summary>
    /// Tries to read the Cinepassion ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadCinepassion(XElement element)
    {
      // Example of a valid element:
      // <cinepassion>52719</cinepassion>
      return ((CurrentStub.Cinepassion = ParseSimpleInt(element)) != null);
    }

    /// <summary>
    /// Tries to read Ids values
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadIds(XElement element)
    {
      // Example of a valid element:
      // <ids>
      //   <entry>
      //     <key>imdbId</key>
      //     <value xsi:type="xs:string" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema" xmlns:xsi="8769http://www.w3.org/2001/XMLSchema-instance">8769</value>
      //   </entry>
      //   <entry>
      //     <key>tmdbId</key>
      //     <value xsi:type="xs:int" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:xsi="8769http://www.w3.org/2001/XMLSchema-instance">8769</value>
      //   </entry>
      // </ids>
      if (element == null || !element.HasElements)
        return false;
      var result = false;
      foreach (var childElement in element.Elements())
      {
        if (childElement.Name == "entry")
        {
          var keyString = ParseSimpleString(childElement.Element("key"));
          switch (keyString)
          {
            case null:
              DebugLogger.Warn("[#{0}]: Key element missing {1}", MiNumber, childElement);
              break;
            case "imdbId":
              result = ((CurrentStub.IdsImdbId = ParseSimpleString(childElement.Element("value"))) != null);
              break;
            case "tmdbId":
              result = ((CurrentStub.IdsTmdbId = ParseSimpleInt(childElement.Element("value"))) != null) || result;
              break;
            default:
              DebugLogger.Warn("[#{0}]: Unknown Key element {1}", MiNumber, childElement);
              break;
          }
        }
        else
          DebugLogger.Warn("[#{0}]: Unknown child element: {1}", MiNumber, childElement);
      }
      return result;
    }

    #endregion

    #region Title information

    /// <summary>
    /// Tries to read the title
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadTitle(XElement element)
    {
      // Example of a valid element:
      // <title>Harry Potter und der Orden des Phönix</title>
      return ((CurrentStub.Title = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the original title
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadOriginalTitle(XElement element)
    {
      // Example of a valid element:
      // <originaltitle>Harry Potter and the Order of the Phoenix</originaltitle>
      return ((CurrentStub.OriginalTitle = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the sort title
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadSortTitle(XElement element)
    {
      // Example of a valid element:
      // <sorttitle>Harry Potter Collection05</sorttitle>
      return ((CurrentStub.SortTitle = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to (asynchronously) read the set information
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadSetAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // Examples of valid elements:
      // 1:
      // <set order = "1">Set Name</set>
      // 2:
      // <set order = "1">
      //   <setname>Harry Potter</setname>
      //   <setdescription>Magician ...</setdescription>
      //   <setrule></setrule>
      //   <setimage></setimage>
      // </set>
      // The order attribute in both cases is optional.
      // In example 2 only the <setname> child element is mandatory.
      if (element == null)
        return false;

      var value = new SetStub();
      // Example 1:
      if (!element.HasElements)
        value.Name = ParseSimpleString(element);
      // Example 2:
      else
      {
        value.Name = ParseSimpleString(element.Element("setname"));
        value.Description = ParseSimpleString(element.Element("setdescription"));
        value.Rule = ParseSimpleString(element.Element("setrule"));
        value.Image = await ParseSimpleImageAsync(element.Element("setimage"), nfoDirectoryFsra);
      }
      value.Order = ParseIntAttribute(element, "order");

      if (value.Name == null)
        return false;

      if (CurrentStub.Sets == null)
        CurrentStub.Sets = new HashSet<SetStub>();
      CurrentStub.Sets.Add(value);
      return true;
    }

    /// <summary>
    /// Tries to (asynchronously) read the sets information
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadSetsAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // Example of a valid element:
      // <sets>
      //   [any number of set elements that can be read by TryReadSetAsync]
      // </sets>
      if (element == null || !element.HasElements)
        return false;
      var result = false;
      foreach (var childElement in element.Elements())
        if (childElement.Name == "set")
        {
          if (await TryReadSetAsync(childElement, nfoDirectoryFsra))
            result = true;
        }
        else
          DebugLogger.Warn("[#{0}]: Unknown child element: {1}", MiNumber, childElement);
      return result;
    }

    #endregion

    #region Making-of information

    /// <summary>
    /// Tries to read the premiered value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadPremiered(XElement element)
    {
      // Examples of valid elements:
      // <premiered>1994-09-14</premiered>
      // <premiered>1994</premiered>
      return ((CurrentStub.Premiered = ParseSimpleDateTime(element)) != null);
    }

    /// <summary>
    /// Tries to read the year value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadYear(XElement element)
    {
      // Examples of valid elements:
      // <year>1994-09-14</year>
      // <year>1994</year>
      return ((CurrentStub.Year = ParseSimpleDateTime(element)) != null);
    }

    /// <summary>
    /// Tries to read a country value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadCountry(XElement element)
    {
      // Examples of valid elements:
      // <country>DE</country>
      // <country>US, GB</country>
      return ((CurrentStub.Countries = ParseCharacterSeparatedStrings(element, CurrentStub.Countries)) != null);
    }

    /// <summary>
    /// Tries to read a company value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadCompany(XElement element)
    {
      // Examples of valid elements:
      // <company>Warner Bros. Pictures</company>
      // <company>Happy Madison Productions / Columbia Pictures</company>
      return ((CurrentStub.Companies = ParseCharacterSeparatedStrings(element, CurrentStub.Companies)) != null);
    }

    /// <summary>
    /// Tries to read a studio or studios value
    /// </summary>
    /// <param name="element">Element to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadStudio(XElement element)
    {
      // Examples of valid elements:
      // <studio>Warner Bros. Pictures</studio>
      // <studio>Happy Madison Productions / Columbia Pictures</studio>
      // <studios>Warner Bros. Pictures</studios>
      // <studios>Happy Madison Productions / Columbia Pictures</studios>
      return ((CurrentStub.Studios = ParseCharacterSeparatedStrings(element, CurrentStub.Studios)) != null);
    }

    /// <summary>
    /// Tries to (asynchronously) read an actor value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadActorAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment in NfoReaderBase.ParsePerson
      var person = await ParsePerson(element, nfoDirectoryFsra);
      if (person == null)
        return false;
      if (CurrentStub.Actors == null)
        CurrentStub.Actors = new HashSet<PersonStub>();
      CurrentStub.Actors.Add(person);
      return true;
    }

    /// <summary>
    /// Tries to (asynchronously) read a producer value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadProducerAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment in NfoReaderBase.ParsePerson
      var person = await ParsePerson(element, nfoDirectoryFsra);
      if (person == null)
        return false;
      if (CurrentStub.Producers == null)
        CurrentStub.Producers = new HashSet<PersonStub>();
      CurrentStub.Producers.Add(person);
      return true;
    }

    /// <summary>
    /// Tries to read the director value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadDirector(XElement element)
    {
      // Example of a valid element:
      // <director>Dennis Dugan</director>
      return ((CurrentStub.Director = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the directorimdb value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadDirectorImdb(XElement element)
    {
      // Example of a valid element:
      // <directorimdb>nm0240797</directorimdb>
      return ((CurrentStub.DirectorImdb = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read a credits value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadCredits(XElement element)
    {
      // Examples of valid elements:
      // <credits>Adam Sandler</credits>
      // <credits>Adam Sandler / Steve Koren</credits>
      return ((CurrentStub.Credits = ParseCharacterSeparatedStrings(element, CurrentStub.Credits)) != null);
    }

    #endregion

    #region Content information

    /// <summary>
    /// Tries to read the plot value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadPlot(XElement element)
    {
      // Example of a valid element:
      // <plot>This movie tells a story about...</plot>
      return ((CurrentStub.Plot = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the outline value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadOutline(XElement element)
    {
      // Example of a valid element:
      // <outline>This movie tells a story about...</outline>
      return ((CurrentStub.Outline = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the tagline value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadTagline(XElement element)
    {
      // Example of a valid element:
      // <tagline>This movie tells a story about...</tagline>
      return ((CurrentStub.Tagline = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the trailer value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadTrailer(XElement element)
    {
      // Example of a valid element:
      // <trailer>This movie tells a story about...</trailer>
      return ((CurrentStub.Trailer = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read a genre value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadGenre(XElement element)
    {
      // Examples of valid elements:
      // <genre>Horror</genre>
      // <genre>Horror / Trash</genre>
      return ((CurrentStub.Genres = ParseCharacterSeparatedStrings(element, CurrentStub.Genres)) != null);
    }

    /// <summary>
    /// Tries to read a genres value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadGenres(XElement element)
    {
      // Example of a valid element:
      // <genres>
      //  <genre>[genre-value]</genre>
      // </genres>
      // There can be one of more <genre> child elements
      // [genre-value] can be any value that can be read by TryReadGenre
      if (element == null || !element.HasElements)
        return false;
      var result = false;
      foreach (var childElement in element.Elements())
      {
        if (childElement.Name == "genre")
          result = TryReadGenre(childElement) || result;
        else
          DebugLogger.Warn("[#{0}]: Unknown child element: {1}", MiNumber, childElement);
      }
      return result;
    }

    /// <summary>
    /// Tries to read a language or languages value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadLanguage(XElement element)
    {
      // Examples of valid elements:
      // <language>de</language>
      // <language>de, en</language>
      // <languages>de</languages>
      // <languages>de, en</languages>
      return ((CurrentStub.Languages = ParseCharacterSeparatedStrings(element, CurrentStub.Languages)) != null);
    }

    #endregion

    #region Images

    /// <summary>
    /// Tries to (asynchronously) read the thumbnail image
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadThumbAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment of NfoReaderBase.ParseSimpleImageAsync
      return ((CurrentStub.Thumb = await ParseSimpleImageAsync(element, nfoDirectoryFsra)) != null);
    }

    /// <summary>
    /// Tries to (asynchronously) read one or more fanart images
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadFanArtAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values c of NfoReaderBase.ParseMultipleImagesAsync
      return ((CurrentStub.FanArt = await ParseMultipleImagesAsync(element, CurrentStub.FanArt, nfoDirectoryFsra)) != null);
    }

    /// <summary>
    /// Tries to (asynchronously) read one or more discart images
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadDiscArtAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment of NfoReaderBase.ParseMultipleImagesAsync
      return ((CurrentStub.DiscArt = await ParseMultipleImagesAsync(element, CurrentStub.DiscArt, nfoDirectoryFsra)) != null);
    }

    /// <summary>
    /// Tries to (asynchronously) read one or more logo images
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadLogoAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment of NfoReaderBase.ParseMultipleImagesAsync
      return ((CurrentStub.Logos = await ParseMultipleImagesAsync(element, CurrentStub.Logos, nfoDirectoryFsra)) != null);
    }

    /// <summary>
    /// Tries to read one or more clearart images
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadClearArtAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment of NfoReaderBase.ParseMultipleImagesAsync
      return ((CurrentStub.ClearArt = await ParseMultipleImagesAsync(element, CurrentStub.ClearArt, nfoDirectoryFsra)) != null);
    }

    /// <summary>
    /// Tries to (asynchronously) read one or more banner images
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadBannerAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment of NfoReaderBase.ParseMultipleImagesAsync
      return ((CurrentStub.Banners = await ParseMultipleImagesAsync(element, CurrentStub.Banners, nfoDirectoryFsra)) != null);
    }

    #endregion

    #region Certification and ratings

    /// <summary>
    /// Tries to read a certification value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadCertification(XElement element)
    {
      // Examples of valid elements:
      // <certification>12</certification>
      // <certification>DE:FSK 12</certification>
      // <certification>DE:FSK 12 / DE:FSK12 / DE:12 / DE:ab 12</certification>
      return ((CurrentStub.Certification = ParseCharacterSeparatedStrings(element, CurrentStub.Certification)) != null);
    }

    /// <summary>
    /// Tries to read a mpaa value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadMpaa(XElement element)
    {
      // Examples of valid elements:
      // <mpaa>12</mpaa>
      // <mpaa>DE:FSK 12</mpaa>
      // <mpaa>DE:FSK 12 / DE:FSK12 / DE:12 / DE:ab 12</mpaa>
      return ((CurrentStub.Mpaa = ParseCharacterSeparatedStrings(element, CurrentStub.Mpaa)) != null);
    }

    /// <summary>
    /// Tries to read a ratings value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadRatings(XElement element)
    {
      // Example of a valid element:
      // <ratings>
      //   <rating moviedb="imdb">8.7</rating>
      // </ratings>
      // The <ratings> element can contain one or more <rating> child elements
      // A value of 0 (zero) is ignored
      if (element == null || !element.HasElements)
        return false;
      var result = false;
      foreach (var childElement in element.Elements())
        if (childElement.Name == "rating")
        {
          var moviedb = ParseStringAttribute(childElement, "moviedb");
          var rating = ParseSimpleDecimal(element);
          if (moviedb != null && rating != null && rating.Value != decimal.Zero)
          {
            result = true;
            if (CurrentStub.Ratings == null)
              CurrentStub.Ratings = new Dictionary<string, decimal>();
            CurrentStub.Ratings[moviedb] = rating.Value;
          }
        }
        else
          DebugLogger.Warn("[#{0}]: Unknown child element: {1}", MiNumber, childElement);
      return result;
    }

    /// <summary>
    /// Tries to read the rating value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadRating(XElement element)
    {
      // Example of a valid element:
      // <rating>8.5</rating>
      // A value of 0 (zero) is ignored
      var value = ParseSimpleDecimal(element);
      if (value == null || value.Value == decimal.Zero)
        return false;
      CurrentStub.Rating = value;
      return true;
    }

    /// <summary>
    /// Tries to read the votes value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadVotes(XElement element)
    {
      // Example of a valid element:
      // <votes>2941</votes>
      // A value of 0 (zero) is ignored
      var value = ParseSimpleInt(element);
      if (value == 0)
        value = null;
      return ((CurrentStub.Votes = value) != null);
    }

    /// <summary>
    /// Tries to read the review value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadReview(XElement element)
    {
      // Example of a valid element:
      // <review>This movie is great...</review>
      return ((CurrentStub.Review = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the top250 value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadTop250(XElement element)
    {
      // Examples of valid elements:
      // <top250>250</top250>
      // <top250>0</top250>
      // A value of 0 (zero) is ignored
      var value = ParseSimpleInt(element);
      if (value == 0)
        value = null;
      return ((CurrentStub.Top250 = value) != null);
    }

    #endregion

    #region Media file information

    /// <summary>
    /// Tries to read the runtime value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadRuntime(XElement element)
    {
      // Examples of valid elements:
      // 1:
      // <runtime>120</runtime>
      // 2:
      // <runtime>2h 00mn</runtime>
      // 3:
      // <runtime>2h 00min</runtime>
      // The value in Example 1 may have decimal places and is interpreted as number of minutes
      // A value of less than 1 second is ignored
      var runtimeString = ParseSimpleString(element);
      if (runtimeString == null)
        return false;

      double runtimeDouble;
      if (double.TryParse(runtimeString, out runtimeDouble))
      {
        // Example 1
        if (runtimeDouble < (1.0 / 60.0))
          return false;
        CurrentStub.Runtime = TimeSpan.FromMinutes(runtimeDouble);
        return true;
      }

      var match = Regex.Match(runtimeString, @"(\d+)\s*h\s*(\d+)\s*[mn|min]", RegexOptions.IgnoreCase);
      if (!match.Success)
        return false;
      
      // Examples 2 and 3
      var hours = int.Parse(match.Groups[1].Value);
      var minutes = int.Parse(match.Groups[2].Value);
      CurrentStub.Runtime = new TimeSpan(hours, minutes, 0);
      return true;
    }

    /// <summary>
    /// Tries to read the fps value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadFps(XElement element)
    {
      // Examples of valid elements:
      // <fps>25</fps>
      // <fps>23.976<fps>
      return ((CurrentStub.Fps = ParseSimpleDecimal(element)) != null);
    }

    /// <summary>
    /// Tries to read the rip value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadRip(XElement element)
    {
      // Example of a valid element:
      // <rip>Bluray</rip>
      return ((CurrentStub.Rip = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the fileinfo value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadFileInfo(XElement element)
    {
      // Example of a valid element:
      // <fileinfo>
      //   <streamdetails>
      //     <video>
      //       <codec>h264</codec>
      //       <aspect>2.4</aspect>
      //       <width>1280</width>
      //       <height>534</height>
      //       <duration>149</duration>
      //       <durationinseconds>8940</durationinseconds>
      //       <stereomode></stereomode>
      //     </video>
      //     <audio>
      //       <codec>ac3</codec>
      //       <language>French</language>
      //       <channels>6</channels>
      //     </audio>
      //     <subtitle>
      //       <language>French</language>
      //     </subtitle>
      //   </streamdetails>
      // </fileinfo>
      // There can be multiple <streamdetails> elements in the <fileinfo> element.
      // There can be multiple <video>, <audio> and/or <subtitle> elements in one <streamdetails> element.
      // the <durationinseconds> element is preferred over the <duration> element.
      if (element == null || !element.HasElements)
        return false;

      var fileInfoFound = false;
      foreach (var stream in element.Elements())
      {
        if (stream.Name != "streamdetails" || !stream.HasElements)
        {
          DebugLogger.Warn("[#{0}]: Unknown or empty child element {1}", MiNumber, stream);
          continue;
        }
        var streamDetails = new StreamDetailsStub();
        var streamValueFound = false;
        foreach (var streamDetail in stream.Elements())
        {
          switch (streamDetail.Name.ToString())
          {
            case "video":
              var videoDetails = new VideoStreamDetailsStub();
              var videoDetailValueFound = ((videoDetails.Codec = ParseSimpleString(streamDetail.Element("codec"))) != null);
              videoDetailValueFound = ((videoDetails.Aspect = ParseSimpleDecimal(streamDetail.Element("aspect"))) != null) || videoDetailValueFound;
              videoDetailValueFound = ((videoDetails.Width = ParseSimpleInt(streamDetail.Element("width"))) != null) || videoDetailValueFound;
              videoDetailValueFound = ((videoDetails.Height = ParseSimpleInt(streamDetail.Element("height"))) != null) || videoDetailValueFound;
              var durationMinutes = ParseSimpleInt(streamDetail.Element("duration"));
              if (durationMinutes != null && durationMinutes > 0)
              {
                videoDetails.Duration = new TimeSpan(0, durationMinutes.Value, 0);
                videoDetailValueFound = true;
              }
              var durationSeconds = ParseSimpleInt(streamDetail.Element("durationinseconds"));
              if (durationSeconds != null && durationSeconds > 0)
              {
                videoDetails.Duration = new TimeSpan(0, 0, durationSeconds.Value);
                videoDetailValueFound = true;
              }
              videoDetailValueFound = ((videoDetails.Stereomode = ParseSimpleString(streamDetail.Element("stereomode"))) != null) || videoDetailValueFound;

              if (videoDetailValueFound)
              {
                if (streamDetails.VideoStreams == null)
                  streamDetails.VideoStreams = new HashSet<VideoStreamDetailsStub>();
                streamDetails.VideoStreams.Add(videoDetails);
                streamValueFound = true;
              }
              break;
            case "audio":
              var audioDetails = new AudioStreamDetailsStub();
              var audioDetailValueFound = ((audioDetails.Codec = ParseSimpleString(streamDetail.Element("codec"))) != null);
              audioDetailValueFound = ((audioDetails.Language = ParseSimpleString(streamDetail.Element("language"))) != null) || audioDetailValueFound;
              audioDetailValueFound = ((audioDetails.Channels = ParseSimpleInt(streamDetail.Element("channels"))) != null) || audioDetailValueFound;
              if (audioDetailValueFound)
              {
                if (streamDetails.AudioStreams == null)
                  streamDetails.AudioStreams = new HashSet<AudioStreamDetailsStub>();
                streamDetails.AudioStreams.Add(audioDetails);
                streamValueFound = true;
              }
              break;
            case "subtitle":
              var subtitleDetails = new SubtitleStreamDetailsStub();
              var subtitleDetailValueFound = ((subtitleDetails.Language = ParseSimpleString(streamDetail.Element("language"))) != null);
              if (subtitleDetailValueFound)
              {
                if (streamDetails.SubtitleStreams == null)
                  streamDetails.SubtitleStreams = new HashSet<SubtitleStreamDetailsStub>();
                streamDetails.SubtitleStreams.Add(subtitleDetails);
                streamValueFound = true;
              }
              break;
            default:
              DebugLogger.Warn("[#{0}]: Unknown child element: {1}", MiNumber, streamDetail);
              break;
          }
        }
        if (streamValueFound)
        {
          if (CurrentStub.FileInfo == null)
            CurrentStub.FileInfo = new HashSet<StreamDetailsStub>();
          CurrentStub.FileInfo.Add(streamDetails);
          fileInfoFound = true;
        }
      }
      return fileInfoFound;
    }

    /// <summary>
    /// Tries to read the epbookmark value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadEpBookmark(XElement element)
    {
      // Example of a valid element:
      // <epbookmark>1200.000000</epbookmark>
      // The value is interpreted as number of seconds; a value of 0 is ignored
      var decimalSeconds = ParseSimpleDecimal(element);
      if (decimalSeconds.HasValue && decimalSeconds != decimal.Zero)
      {
        CurrentStub.EpBookmark = TimeSpan.FromSeconds((double)decimalSeconds);
        return true;
      }
      return false;
    }

    #endregion

    #region User information

    /// <summary>
    /// Tries to read the watched value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadWatched(XElement element)
    {
      // Example of a valid element:
      // <watched>true</watched>
      var watchedString = ParseSimpleString(element);
      bool watched;
      if (bool.TryParse(watchedString, out watched))
      {
        CurrentStub.Watched = watched;
        return true;
      }
      DebugLogger.Warn("[#{0}]: The following elelement was supposed to contain a bool value but it does not: {1}", MiNumber, element);
      return false;
    }

    /// <summary>
    /// Tries to read the playcount value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadPlayCount(XElement element)
    {
      // Example of a valid element:
      // <playcount>3</playcount>
      return ((CurrentStub.PlayCount = ParseSimpleInt(element)) != null);
    }

    /// <summary>
    /// Tries to read the lastplayed value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadLastPlayed(XElement element)
    {
      // Example of a valid element:
      // <lastplayed>2013-10-08 21:46</lastplayed>
      return ((CurrentStub.LastPlayed = ParseSimpleDateTime(element)) != null);
    }

    /// <summary>
    /// Tries to read the dateadded value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadDateAdded(XElement element)
    {
      // Example of a valid element:
      // <dateadded>2013-10-08 21:46</dateadded>
      return ((CurrentStub.DateAdded = ParseSimpleDateTime(element)) != null);
    }

    /// <summary>
    /// Tries to read the resume value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadResume(XElement element)
    {
      // Example of a valid element:
      // <resume>
      //   <position>3600.000000</position>
      //   <total>5400.000000</total>
      // </resume>
      // The <total> child element (as well as any other child element of the <resume> element) is ignored.
      // THe <position> value is taken and interpreted as number of seconds; a value of less than 1 second is ignored.
      if (element == null || !element.HasElements)
        return false;
      var resumeDecimal = ParseSimpleDecimal(element.Element("position"));
      if (resumeDecimal == null || resumeDecimal < decimal.One)
        return false;
      CurrentStub.ResumePosition = TimeSpan.FromSeconds((double)resumeDecimal);
      return true;
    }

    #endregion

    #endregion

    #region Writer methods to store metadata in MediaItemAspects

    // The following writer methods only write the first item found in the nfo-file
    // into the MediaItemAspects. This can be extended in the future once we support
    // multiple MediaItems in one media file.

    #region MediaAspect

    /// <summary>
    /// Tries to write metadata into <see cref="MediaAspect.ATTR_TITLE"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMediaAspectTitle(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      if (Stubs[0].Title != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, Stubs[0].Title);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MediaAspect.ATTR_RECORDINGTIME"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMediaAspectRecordingTime(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      // priority 1:
      if (Stubs[0].Premiered.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, Stubs[0].Premiered.Value);
        return true;
      }
      //priority 2:
      if (Stubs[0].Year.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, Stubs[0].Year.Value);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MediaAspect.ATTR_PLAYCOUNT"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMediaAspectPlayCount(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      //priority 1:
      if (Stubs[0].PlayCount.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_PLAYCOUNT, Stubs[0].PlayCount.Value);
        return true;
      }
      //priority 2:
      if (Stubs[0].Watched.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_PLAYCOUNT, Stubs[0].Watched.Value ? 1 : 0);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MediaAspect.ATTR_LASTPLAYED"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMediaAspectLastPlayed(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      if (Stubs[0].LastPlayed.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_LASTPLAYED, Stubs[0].LastPlayed.Value);
        return true;
      }
      return false;
    }

    #endregion

    #region VideoAspect

    /// <summary>
    /// Tries to write metadata into <see cref="VideoAspect.ATTR_GENRES"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteVideoAspectGenres(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      if (Stubs[0].Genres != null && Stubs[0].Genres.Any())
      {
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_GENRES, Stubs[0].Genres.ToList());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="VideoAspect.ATTR_ACTORS"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteVideoAspectActors(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      if (Stubs[0].Actors != null && Stubs[0].Actors.Any())
      {
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_ACTORS, Stubs[0].Actors.OrderBy(actor => actor.Order).Select(actor => actor.Name).ToList());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="VideoAspect.ATTR_DIRECTORS"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteVideoAspectDirectors(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      if (Stubs[0].Director != null)
      {
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_DIRECTORS, new List<string> { Stubs[0].Director });
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="VideoAspect.ATTR_STORYPLOT"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteVideoAspectStoryPlot(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      // priority 1:
      if (Stubs[0].Plot != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, Stubs[0].Plot);
        return true;
      }
      // priority 2:
      if (Stubs[0].Outline != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, Stubs[0].Outline);
        return true;
      }
      return false;
    }

    #endregion

    #region MovieAspect

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_MOVIE_NAME"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectMovieName(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      if (Stubs[0].Title != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_MOVIE_NAME, Stubs[0].Title);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_ORIG_MOVIE_NAME"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectOrigName(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      if (Stubs[0].OriginalTitle != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_ORIG_MOVIE_NAME, Stubs[0].OriginalTitle);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_TMDB_ID"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectTmdbId(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      //priority 1:
      if (Stubs[0].TmdbId.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_TMDB_ID, Stubs[0].TmdbId.Value);
        return true;
      }
      //priority 2:
      if (Stubs[0].Thmdb.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_TMDB_ID, Stubs[0].Thmdb.Value);
        return true;
      }
      //priority 3:
      if (Stubs[0].IdsTmdbId.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_TMDB_ID, Stubs[0].IdsTmdbId.Value);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_IMDB_ID"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectImdbId(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      //priority 1:
      if (Stubs[0].Id != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_IMDB_ID, Stubs[0].Id);
        return true;
      }
      //priority 2:
      if (Stubs[0].Imdb != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_IMDB_ID, Stubs[0].Imdb);
        return true;
      }
      //priority 3:
      if (Stubs[0].IdsImdbId != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_IMDB_ID, Stubs[0].Imdb);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_COLLECTION_NAME"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectCollectionName(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      if (Stubs[0].Sets != null && Stubs[0].Sets.Any())
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_COLLECTION_NAME, Stubs[0].Sets.OrderBy(set => set.Order).First().Name);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_RUNTIME_M"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectRuntime(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      if (Stubs[0].Runtime.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_RUNTIME_M, (int)Stubs[0].Runtime.Value.TotalMinutes);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_CERTIFICATION"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectCertification(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      // priority 1:
      if (Stubs[0].Certification != null && Stubs[0].Certification.Any())
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_CERTIFICATION, Stubs[0].Certification.First());
        return true;
      }
      // priority 2:
      if (Stubs[0].Mpaa != null && Stubs[0].Mpaa.Any())
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_CERTIFICATION, Stubs[0].Mpaa.First());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_TAGLINE"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectTagline(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      if (Stubs[0].Tagline != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_TAGLINE, Stubs[0].Tagline);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_TOTAL_RATING"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectTotalRating(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      // priority 1:
      if (Stubs[0].Rating.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_TOTAL_RATING, (double)Stubs[0].Rating.Value);
        return true;
      }
      // priority 2:
      if (Stubs[0].Ratings != null && Stubs[0].Ratings.Any())
      {
        decimal rating;
        if (!Stubs[0].Ratings.TryGetValue("imdb", out rating))
          rating = Stubs[0].Ratings.First().Value;
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_TOTAL_RATING, (double)rating);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_RATING_COUNT"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectRatingCount(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      if (Stubs[0].Votes.HasValue)
      {
        if (Stubs[0].Rating.HasValue || (Stubs[0].Ratings != null && Stubs[0].Ratings.Count == 1))
        {
          MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_RATING_COUNT, Stubs[0].Votes.Value);
          return true;
        }
      }
      return false;
    }

    #endregion

    #region ThumbnailLargeAspect

    /// <summary>
    /// Tries to write metadata into <see cref="ThumbnailLargeAspect.ATTR_THUMBNAIL"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteThumbnailLargeAspectThumbnail(IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      if (Stubs[0].Thumb != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, Stubs[0].Thumb);
        return true;
      }
      return false;
    }

    #endregion

    #endregion

    #region General helper methods

    /// <summary>
    /// Ignores the respective element
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to ignore</param>
    /// <returns><c>false</c></returns>
    /// <remarks>
    /// We use this method as TryReadElementDelegate for elements, of which we know that they are irrelevant in the context of a movie,
    /// but which are nevertheless contained in some movie's nfo-files. Having this method registered as handler delegate avoids that
    /// the respective xml element is logged as unknown element.
    /// </remarks>
    private static bool Ignore(XElement element)
    {
      return false;
    }

    #endregion

    #endregion

    #region BaseOverrides

    /// <summary>
    /// Checks whether the <paramref name="itemRootElement"/>'s name is "movie"
    /// </summary>
    /// <param name="itemRootElement">Element to check</param>
    /// <returns><c>true</c> if the element's name is "movie"; else <c>false</c></returns>
    protected override bool CanReadItemRootElementTree(XElement itemRootElement)
    {
      var itemRootElementName = itemRootElement.Name.ToString();
      if (itemRootElementName == MOVIE_ROOT_ELEMENT_NAME)
        return true;
      DebugLogger.Warn("[#{0}]: Cannot extract metadata; name of the item root element is {1} instead of {2}", MiNumber, itemRootElementName, MOVIE_ROOT_ELEMENT_NAME);
      return false;
    }

    #endregion
  }
}
