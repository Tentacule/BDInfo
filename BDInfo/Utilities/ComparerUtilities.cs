using BDInfo.BDROM;

namespace BDInfo.Utilities
{
    public abstract class ComparerUtilities
    {
        public static int CompareStreamFiles(TSStreamFile x, TSStreamFile y)
        {
            // TODO: Use interleaved file sizes

            if ((x == null || x.FileInfo == null) && (y == null || y.FileInfo == null))
            {
                return 0;
            }
            else if ((x == null || x.FileInfo == null) && (y != null && y.FileInfo != null))
            {
                return 1;
            }
            else if ((x != null || x.FileInfo != null) && (y == null || y.FileInfo == null))
            {
                return -1;
            }
            else
            {
                if (x.FileInfo.Length > y.FileInfo.Length)
                {
                    return 1;
                }
                else if (y.FileInfo.Length > x.FileInfo.Length)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public static int ComparePlaylistFiles(TSPlaylistFile x, TSPlaylistFile y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null && y != null)
            {
                return 1;
            }
            else if (x != null && y == null)
            {
                return -1;
            }
            else
            {
                if (x.TotalLength > y.TotalLength)
                {
                    return -1;
                }
                else if (y.TotalLength > x.TotalLength)
                {
                    return 1;
                }
                else
                {
                    return x.Name.CompareTo(y.Name);
                }
            }
        }

        public static int CompareVideoStreams(TSVideoStream x, TSVideoStream y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null && y != null)
            {
                return 1;
            }
            else if (x != null && y == null)
            {
                return -1;
            }
            else
            {
                if (x.Height > y.Height)
                {
                    return -1;
                }
                else if (y.Height > x.Height)
                {
                    return 1;
                }
                else if (x.PID > y.PID)
                {
                    return 1;
                }
                else if (y.PID > x.PID)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public static int CompareAudioStreams(TSAudioStream x, TSAudioStream y)
        {
            if (x == y)
            {
                return 0;
            }
            else if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null && y != null)
            {
                return -1;
            }
            else if (x != null && y == null)
            {
                return 1;
            }
            else
            {
                if (x.ChannelCount > y.ChannelCount)
                {
                    return -1;
                }
                else if (y.ChannelCount > x.ChannelCount)
                {
                    return 1;
                }
                else
                {
                    int sortX = GetStreamTypeSortIndex(x.StreamType);
                    int sortY = GetStreamTypeSortIndex(y.StreamType);

                    if (sortX > sortY)
                    {
                        return -1;
                    }
                    else if (sortY > sortX)
                    {
                        return 1;
                    }
                    else
                    {
                        if (x.LanguageCode == "eng")
                        {
                            return -1;
                        }
                        else if (y.LanguageCode == "eng")
                        {
                            return 1;
                        }
                        else if (x.LanguageCode != y.LanguageCode)
                        {
                            return string.Compare(
                                x.LanguageName, y.LanguageName);
                        }
                        else if (x.PID < y.PID)
                        {
                            return -1;
                        }
                        else if (y.PID < x.PID)
                        {
                            return 1;
                        }
                        return 0;
                    }
                }
            }
        }

        public static int CompareTextStreams(TSTextStream x, TSTextStream y)
        {
            if (x == y)
            {
                return 0;
            }
            else if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null && y != null)
            {
                return -1;
            }
            else if (x != null && y == null)
            {
                return 1;
            }
            else
            {
                if (x.LanguageCode == "eng")
                {
                    return -1;
                }
                else if (y.LanguageCode == "eng")
                {
                    return 1;
                }
                else
                {
                    if (x.LanguageCode == y.LanguageCode)
                    {
                        if (x.PID > y.PID)
                        {
                            return 1;
                        }
                        else if (y.PID > x.PID)
                        {
                            return -1;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        return string.Compare(
                            x.LanguageName, y.LanguageName);
                    }
                }
            }
        }

        public static int CompareGraphicsStreams(TSGraphicsStream x, TSGraphicsStream y)
        {
            if (x == y)
            {
                return 0;
            }
            else if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null && y != null)
            {
                return -1;
            }
            else if (x != null && y == null)
            {
                return 1;
            }
            else
            {
                int sortX = GetStreamTypeSortIndex(x.StreamType);
                int sortY = GetStreamTypeSortIndex(y.StreamType);

                if (sortX > sortY)
                {
                    return -1;
                }
                else if (sortY > sortX)
                {
                    return 1;
                }
                else if (x.LanguageCode == "eng")
                {
                    return -1;
                }
                else if (y.LanguageCode == "eng")
                {
                    return 1;
                }
                else
                {
                    if (x.LanguageCode == y.LanguageCode)
                    {
                        if (x.PID > y.PID)
                        {
                            return 1;
                        }
                        else if (y.PID > x.PID)
                        {
                            return -1;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        return string.Compare(x.LanguageName, y.LanguageName);
                    }
                }
            }
        }

        private static int GetStreamTypeSortIndex(TSStreamType streamType)
        {
            switch (streamType)
            {
                case TSStreamType.Unknown:
                    return 0;
                case TSStreamType.MPEG1_VIDEO:
                    return 1;
                case TSStreamType.MPEG2_VIDEO:
                    return 2;
                case TSStreamType.AVC_VIDEO:
                    return 3;
                case TSStreamType.VC1_VIDEO:
                    return 4;
                case TSStreamType.MVC_VIDEO:
                    return 5;

                case TSStreamType.MPEG1_AUDIO:
                    return 1;
                case TSStreamType.MPEG2_AUDIO:
                    return 2;
                case TSStreamType.AC3_PLUS_SECONDARY_AUDIO:
                    return 3;
                case TSStreamType.DTS_HD_SECONDARY_AUDIO:
                    return 4;
                case TSStreamType.AC3_AUDIO:
                    return 5;
                case TSStreamType.DTS_AUDIO:
                    return 6;
                case TSStreamType.AC3_PLUS_AUDIO:
                    return 7;
                case TSStreamType.DTS_HD_AUDIO:
                    return 8;
                case TSStreamType.AC3_TRUE_HD_AUDIO:
                    return 9;
                case TSStreamType.DTS_HD_MASTER_AUDIO:
                    return 10;
                case TSStreamType.LPCM_AUDIO:
                    return 11;

                case TSStreamType.SUBTITLE:
                    return 1;
                case TSStreamType.INTERACTIVE_GRAPHICS:
                    return 2;
                case TSStreamType.PRESENTATION_GRAPHICS:
                    return 3;

                default:
                    return 0;
            }
        }

        private ComparerUtilities()
        {

        }
    }
}
