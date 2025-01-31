﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using DiffMatchPatch;
using EnvDTE;
using Tridion.ContentManager.CoreService.Client;
using System.Text;
using SDL.TridionVSRazorExtension.Properties;

namespace SDL.TridionVSRazorExtension
{
    public static class Functions
    {
        #region Fields

        public static ILocalClient Client;
        public static StreamDownloadClient StreamDownloadClient;
        public static StreamUploadClient StreamUploadClient;
        public static Project Project;
        public static TextBlock TxtLog;
        public static string RootPath;
        public static BindingType ClientBindingType = BindingType.HttpBinding;
        public static string ClientVersion = "2013";

        private static readonly Dictionary<string, string> Extensions = new Dictionary<string, string>
            {
                {".css", "text/css"},
                {".js", "text/javascript"},
                {".jpg", "image/jpg"},
                {".png", "image/png"},
                {".gif", "image/gif"},
                {".mp4", "video/mp4"},
                {".webm", "video/webm"},
                {".ogv", "video/ogg"},
                {".swf", "application/x-shockwave-flash"},
                {".eot", "application/vnd.ms-fontobject"},
                {".svg", "image/svg+xml"},
                {".ttf", "application/x-font-truetype"},
                {".woff", "application/x-font-woff"},
                {".pdf", "application/pdf"},
                {".doc", "application/msword"},
                {".xls", "application/vnd.ms-excel"},
                {".ppt", "application/vnd.ms-powerpoint"},
                {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
                {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                {".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
                {".kml", "application/vnd.google-earth.kml+xml"},
                {".kmz", "application/vnd.google-earth.kmz"},
                {".zip", "application/x-zip-compressed"},
                {".ico", "image/x-icon"}
            };

        #endregion

        #region Tridion CoreService

        public delegate void CredentialsEventHandler();
        public static event CredentialsEventHandler CredentialsChanged;

        private static void EnsureCredentialsNotEmpty(MappingInfo mapping)
        {
            if (String.IsNullOrEmpty(mapping.Username) || String.IsNullOrEmpty(mapping.Password))
            {
                if (String.IsNullOrEmpty(mapping.Username) || String.IsNullOrEmpty(mapping.Password))
                {
                    PasswordDialogWindow dialog = new PasswordDialogWindow();
                    dialog.Mapping = mapping;
                    bool res = dialog.ShowDialog() == true;
                    if (res)
                    {
                        if (CredentialsChanged != null)
                        {
                            CredentialsChanged();
                        }
                    }
                }
            }
        }

        private static NetTcpBinding GetBinding()
        {
            var binding = new NetTcpBinding
            {
                MaxReceivedMessageSize = 2147483647,
                ReaderQuotas = new XmlDictionaryReaderQuotas
                {
                    MaxStringContentLength = 2097152,
                    MaxArrayLength = 81920,
                    MaxBytesPerRead = 5120,
                    MaxDepth = 32,
                    MaxNameTableCharCount = 81920
                },
                CloseTimeout = TimeSpan.FromMinutes(10),
                OpenTimeout = TimeSpan.FromMinutes(10),
                ReceiveTimeout = TimeSpan.FromMinutes(10),
                SendTimeout = TimeSpan.FromMinutes(10),
                TransactionFlow = true,
                TransactionProtocol = TransactionProtocol.WSAtomicTransaction11
            };
            return binding;
        }

        private static BasicHttpBinding GetHttpBinding()
        {
            var binding = new BasicHttpBinding
            {
                MaxReceivedMessageSize = 2147483647,
                ReaderQuotas = new XmlDictionaryReaderQuotas
                {
                    MaxStringContentLength = 2097152,
                    MaxArrayLength = 81920,
                    MaxBytesPerRead = 5120,
                    MaxDepth = 32,
                    MaxNameTableCharCount = 81920
                },
                CloseTimeout = TimeSpan.FromMinutes(10),
                OpenTimeout = TimeSpan.FromMinutes(10),
                ReceiveTimeout = TimeSpan.FromMinutes(10),
                SendTimeout = TimeSpan.FromMinutes(10),
                MessageEncoding = WSMessageEncoding.Mtom,
                TransferMode = TransferMode.Streamed
            };
            return binding;
        }

        private static BasicHttpBinding GetHttpBinding2()
        {
            var binding = new BasicHttpBinding
            {
                MaxReceivedMessageSize = 2147483647,
                ReaderQuotas = new XmlDictionaryReaderQuotas
                {
                    MaxStringContentLength = 2097152,
                    MaxArrayLength = 81920,
                    MaxBytesPerRead = 5120,
                    MaxDepth = 32,
                    MaxNameTableCharCount = 81920
                },
                CloseTimeout = TimeSpan.FromMinutes(10),
                OpenTimeout = TimeSpan.FromMinutes(10),
                ReceiveTimeout = TimeSpan.FromMinutes(10),
                SendTimeout = TimeSpan.FromMinutes(10),
                Security = new BasicHttpSecurity
                {
                    Mode = BasicHttpSecurityMode.TransportCredentialOnly,
                    Transport = new HttpTransportSecurity
                    {
                        ClientCredentialType = HttpClientCredentialType.Windows
                    },
                },
                MessageEncoding = WSMessageEncoding.Mtom
            };
            return binding;
        }

        private static BasicHttpBinding GetHttpBinding3()
        {
            var binding = new BasicHttpBinding
            {
                MaxReceivedMessageSize = 2147483647,
                ReaderQuotas = new XmlDictionaryReaderQuotas
                {
                    MaxStringContentLength = 2097152,
                    MaxArrayLength = 81920,
                    MaxBytesPerRead = 5120,
                    MaxDepth = 32,
                    MaxNameTableCharCount = 81920
                },
                CloseTimeout = TimeSpan.FromMinutes(10),
                OpenTimeout = TimeSpan.FromMinutes(10),
                ReceiveTimeout = TimeSpan.FromMinutes(10),
                SendTimeout = TimeSpan.FromMinutes(10),
                Security = new BasicHttpSecurity
                {
                    Mode = BasicHttpSecurityMode.TransportCredentialOnly,
                    Transport = new HttpTransportSecurity
                    {
                        ClientCredentialType = HttpClientCredentialType.Windows
                    },
                },
            };
            return binding;
        }

        public static LocalSessionAwareCoreServiceClient GetTcpClient(string host, string username, string password)
        {
            if (String.IsNullOrEmpty(host))
                host = "localhost";

            host = host.GetDomainName();

            var binding = GetBinding();

            var endpoint = new EndpointAddress(String.Format("net.tcp://{0}:2660/CoreService/{1}/netTcp", host, ClientVersion));

            var client = new LocalSessionAwareCoreServiceClient(binding, endpoint);

            if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password))
            {
                client.ChannelFactory.Credentials.Windows.ClientCredential = new NetworkCredential(username, password);
            }

            return client;
        }

        public static LocalSessionAwareCoreServiceClient GetTcpClient(MappingInfo mapping)
        {
            EnsureCredentialsNotEmpty(mapping);
            return GetTcpClient(mapping.Host, mapping.Username, mapping.Password);
        }

        public static LocalCoreServiceClient GetHttpClient(string host, string username, string password)
        {
            if (String.IsNullOrEmpty(host))
                host = "localhost";

            host = host.GetDomainName();

            var binding = GetHttpBinding3();

            var endpoint = new EndpointAddress(String.Format("http://{0}/webservices/CoreService{1}.svc/basicHttp", host, ClientVersion));

            var client = new LocalCoreServiceClient(binding, endpoint);

            if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password))
            {
                client.ClientCredentials.Windows.AllowedImpersonationLevel = TokenImpersonationLevel.Impersonation;
                client.ClientCredentials.Windows.ClientCredential = new NetworkCredential(username, password);
            }

            return client;
        }

        public static LocalCoreServiceClient GetHttpClient(MappingInfo mapping)
        {
            EnsureCredentialsNotEmpty(mapping);
            return GetHttpClient(mapping.Host, mapping.Username, mapping.Password);
        }

        public static bool EnsureValidClient(MappingInfo mapping)
        {
            if (Client == null || Client is SessionAwareCoreServiceClient && ((SessionAwareCoreServiceClient)Client).InnerChannel.State == CommunicationState.Faulted)
            {
                if (ClientBindingType == BindingType.HttpBinding)
                    Client = GetHttpClient(mapping.Host, mapping.Username, mapping.Password);
                else
                    Client = GetTcpClient(mapping.Host, mapping.Username, mapping.Password);

                try
                {
                    var publications = Client.GetSystemWideListXml(new PublicationsFilterData());
                }
                catch (Exception ex)
                {
                    WriteErrorLog("Not able to connect to TCM. Check your credentials and try again", ex.StackTrace);
                    Client = null;
                    return false;
                }
            }
            return true;
        }

        public static void ResetClient()
        {
            Client = null;
        }

        public static StreamDownloadClient GetStreamDownloadClient(string host, string username, string password)
        {
            if (String.IsNullOrEmpty(host))
                host = "localhost";

            host = host.GetDomainName();

            var binding = GetHttpBinding2();

            var endpoint = new EndpointAddress(string.Format("http://{0}/webservices/CoreService{1}.svc/streamDownload_basicHttp", host, ClientVersion));

            StreamDownloadClient client = new StreamDownloadClient(binding, endpoint);

            if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password))
            {
                client.ClientCredentials.Windows.AllowedImpersonationLevel = TokenImpersonationLevel.Impersonation;
                client.ClientCredentials.Windows.ClientCredential = new NetworkCredential(username, password);
            }

            return client;
        }

        public static StreamDownloadClient GetStreamDownloadClient(MappingInfo mapping)
        {
            EnsureCredentialsNotEmpty(mapping);
            return GetStreamDownloadClient(mapping.Host, mapping.Username, mapping.Password);
        }

        private static void EnsureValidStreamDownloadClient(MappingInfo mapping)
        {
            if (StreamDownloadClient == null || StreamDownloadClient.InnerChannel.State == CommunicationState.Faulted)
            {
                StreamDownloadClient = GetStreamDownloadClient(mapping);
            }
        }

        public static void ResetDownloadClient()
        {
            StreamDownloadClient = null;
        }

        public static StreamUploadClient GetStreamUploadClient(string host, string username, string password)
        {
            if (String.IsNullOrEmpty(host))
                host = "localhost";

            host = host.GetDomainName();

            var binding = GetHttpBinding();

            var endpoint = new EndpointAddress(string.Format("http://{0}/webservices/CoreService{1}.svc/streamUpload_basicHttp", host, ClientVersion));

            StreamUploadClient client = new StreamUploadClient(binding, endpoint);

            return client;
        }

        public static StreamUploadClient GetStreamUploadClient(MappingInfo mapping)
        {
            EnsureCredentialsNotEmpty(mapping);
            return GetStreamUploadClient(mapping.Host, mapping.Username, mapping.Password);
        }

        private static void EnsureValidStreamUploadClient(MappingInfo mapping)
        {
            if (StreamUploadClient == null || StreamUploadClient.InnerChannel.State == CommunicationState.Faulted)
            {
                StreamUploadClient = GetStreamUploadClient(mapping);
            }
        }

        public static void ResetUploadClient()
        {
            StreamUploadClient = null;
        }

        public static bool TestConnection(MappingInfo mapping)
        {
            using (TcpClient tcpScan = new TcpClient())
            {
                try
                {
                    tcpScan.Connect(mapping.Host, 80);
                    return true;
                }
                catch (Exception ex)
                {
                    WriteErrorLog("Connecting failed", ex.StackTrace);
                    return false;
                }
            }
        }

        #endregion

        #region Tridion items access

        public static bool SaveRazorLayoutTbb(MappingInfo mapping, string id, string code, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            if (String.IsNullOrEmpty(id))
                return false;

            if(!EnsureValidClient(mapping))
                return false;

            TemplateBuildingBlockData tbbData = ReadItem(mapping, id) as TemplateBuildingBlockData;
            if (tbbData == null)
                return false;

            if (tbbData.BluePrintInfo.IsShared == true)
            {
                id = GetBluePrintTopTcmId(mapping, id);

                tbbData = ReadItem(mapping, id) as TemplateBuildingBlockData;
                if (tbbData == null)
                    return false;
            }

            try
            {
                tbbData = Client.CheckOut(id, true, new ReadOptions()) as TemplateBuildingBlockData;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                return false;
            }

            if (tbbData == null)
                return false;

            tbbData.Content = code;

            try
            {
                tbbData = Client.Update(tbbData, new ReadOptions()) as TemplateBuildingBlockData;
                if (tbbData == null)
                    return false;

                if (tbbData.Content == code)
                {
                    Client.CheckIn(id, new ReadOptions());
                    return true;
                }

                Client.UndoCheckOut(id, true, new ReadOptions());
                return false;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                Client.UndoCheckOut(id, true, new ReadOptions());
                return false;
            }
        }

        public static bool SaveRazorLayoutTbb(MappingInfo mapping, string title, string code, string tcmContainer, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            if (!EnsureValidClient(mapping))
                return false;

            if (ExistsItem(mapping, tcmContainer, title))
            {
                string id = GetItemTcmId(mapping, tcmContainer, title);
                if (String.IsNullOrEmpty(id))
                    return false;

                return SaveRazorLayoutTbb(mapping, id, code, out stackTraceMessage);
            }

            try
            {
                TemplateBuildingBlockData tbbData = new TemplateBuildingBlockData
                {
                    Content = code,
                    Title = title,
                    LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } },
                    Id = "tcm:0-0-0",
                    TemplateType = "RazorTemplate"
                };

                tbbData = Client.Save(tbbData, new ReadOptions()) as TemplateBuildingBlockData;
                if (tbbData == null)
                    return false;

                Client.CheckIn(tbbData.Id, new ReadOptions());
                return true;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                return false;
            }
        }

        public static bool SavePageTemplate(MappingInfo mapping, string title, string xml, string tcmContainer, string fileExtension, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            if (!EnsureValidClient(mapping))
                return false;

            if (ExistsItem(mapping, tcmContainer, title))
            {
                string id = GetItemTcmId(mapping, tcmContainer, title);
                if (String.IsNullOrEmpty(id))
                    return false;

                PageTemplateData templateData = ReadItem(mapping, id) as PageTemplateData;
                if (templateData == null)
                    return false;

                if (templateData.BluePrintInfo.IsShared == true)
                {
                    id = GetBluePrintTopTcmId(mapping, id);

                    templateData = ReadItem(mapping, id) as PageTemplateData;
                    if (templateData == null)
                        return false;
                }

                try
                {
                    templateData = Client.CheckOut(id, true, new ReadOptions()) as PageTemplateData;
                }
                catch (Exception ex)
                {
                    stackTraceMessage = ex.Message;
                    return false;
                }

                if (templateData == null)
                    return false;

                templateData.Content = xml;
                templateData.Title = title;
                templateData.LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } };
                templateData.FileExtension = fileExtension;

                try
                {
                    templateData = Client.Update(templateData, new ReadOptions()) as PageTemplateData;
                    if (templateData == null)
                        return false;

                    if (templateData.Content == xml)
                    {
                        Client.CheckIn(id, new ReadOptions());
                        return true;
                    }

                    Client.UndoCheckOut(id, true, new ReadOptions());
                    return false;
                }
                catch (Exception ex)
                {
                    stackTraceMessage = ex.Message;

                    if (templateData == null)
                        return false;

                    Client.UndoCheckOut(templateData.Id, true, new ReadOptions());
                    return false;
                }
            }

            try
            {
                PageTemplateData templateData = new PageTemplateData
                {
                    Content = xml,
                    Title = title,
                    LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } },
                    Id = "tcm:0-0-0",
                    TemplateType = "CompoundTemplate",
                    FileExtension = fileExtension
                };

                templateData = Client.Save(templateData, new ReadOptions()) as PageTemplateData;
                if (templateData == null)
                    return false;

                Client.CheckIn(templateData.Id, new ReadOptions());
                return true;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                return false;
            }
        }

        public static bool SaveComponentTemplate(MappingInfo mapping, string title, string xml, string tcmContainer, string outputFormat, bool dynamic, out string stackTraceMessage, params string[] allowedSchemaNames)
        {
            stackTraceMessage = "";

            if (!EnsureValidClient(mapping))
                return false;

            List<LinkToSchemaData> schemaList = new List<LinkToSchemaData>();

            string tcmPublication = GetPublicationTcmId(tcmContainer);
            List<ItemInfo> allSchemas = GetSchemas(mapping, tcmPublication);

            if (allowedSchemaNames != null && allowedSchemaNames.Length > 0)
            {
                foreach (string schemaName in allowedSchemaNames)
                {
                    if (allSchemas.Any(x => x.Title == schemaName))
                    {
                        string tcmSchema = allSchemas.First(x => x.Title == schemaName).TcmId;
                        LinkToSchemaData link = new LinkToSchemaData {IdRef = tcmSchema};
                        schemaList.Add(link);
                    }
                }
            }

            if (ExistsItem(mapping, tcmContainer, title))
            {
                string id = GetItemTcmId(mapping, tcmContainer, title);
                if (String.IsNullOrEmpty(id))
                    return false;

                ComponentTemplateData templateData = ReadItem(mapping, id) as ComponentTemplateData;
                if (templateData == null)
                    return false;

                if (templateData.BluePrintInfo.IsShared == true)
                {
                    id = GetBluePrintTopTcmId(mapping, id);

                    templateData = ReadItem(mapping, id) as ComponentTemplateData;
                    if (templateData == null)
                        return false;
                }

                try
                {
                    templateData = Client.CheckOut(templateData.Id, true, new ReadOptions()) as ComponentTemplateData;
                }
                catch (Exception ex)
                {
                    stackTraceMessage = ex.Message;
                    return false;
                }

                if (templateData == null)
                    return false;

                templateData.Content = xml;
                templateData.Title = title;
                templateData.LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } };
                templateData.OutputFormat = outputFormat;
                templateData.RelatedSchemas = schemaList.ToArray();

                try
                {
                    templateData = Client.Update(templateData, new ReadOptions()) as ComponentTemplateData;
                    if (templateData == null)
                        return false;

                    if (templateData.Content == xml)
                    {
                        Client.CheckIn(templateData.Id, new ReadOptions());
                        return true;
                    }

                    Client.UndoCheckOut(templateData.Id, true, new ReadOptions());
                    return false;
                }
                catch (Exception ex)
                {
                    stackTraceMessage = ex.Message;

                    if (templateData == null)
                        return false;

                    Client.UndoCheckOut(templateData.Id, true, new ReadOptions());
                    return false;
                }
            }

            try
            {
                ComponentTemplateData templateData = new ComponentTemplateData
                {
                    Content = xml,
                    Title = title,
                    LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } },
                    Id = "tcm:0-0-0",
                    TemplateType = "CompoundTemplate",
                    OutputFormat = outputFormat,
                    IsRepositoryPublishable = dynamic,
                    AllowOnPage = true,
                    RelatedSchemas = schemaList.ToArray()
                };

                templateData = Client.Save(templateData, new ReadOptions()) as ComponentTemplateData;
                if (templateData == null)
                    return false;

                Client.CheckIn(templateData.Id, new ReadOptions());
                return true;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                return false;
            }
        }

        public static bool CreateFolder(MappingInfo mapping, string title, string tcmContainer)
        {
            if (!EnsureValidClient(mapping))
                return false;

            try
            {
                FolderData folderData = new FolderData
                {
                    Title = title,
                    LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } },
                    Id = "tcm:0-0-0"
                };

                folderData = Client.Save(folderData, new ReadOptions()) as FolderData;
                if (folderData == null)
                    return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string CreateFolderChain(MappingInfo mapping, string folderPath, string tcmContainer)
        {
            if(String.IsNullOrEmpty(folderPath))
                return tcmContainer;

            if (String.IsNullOrEmpty(tcmContainer))
                return tcmContainer;

            string[] chain = folderPath.Trim('\\').Split('\\');
            if(chain.Length == 0)
                return tcmContainer;

            string topFolder = chain[0];
            List<ItemInfo> items = GetFoldersByParentFolder(mapping, tcmContainer);
            if (items.All(x => x.Title != topFolder))
            {
                CreateFolder(mapping, topFolder, tcmContainer);
                items = GetFoldersByParentFolder(mapping, tcmContainer);
            }

            string tcmTopFolder = items.First(x => x.Title == topFolder).TcmId;

            return CreateFolderChain(mapping, string.Join("\\", chain.Skip(1)), tcmTopFolder);
        }

        public static bool CreateStructureGroup(MappingInfo mapping, string title, string tcmContainer)
        {
            if (!EnsureValidClient(mapping))
                return false;

            try
            {
                StructureGroupData sgData = new StructureGroupData
                {
                    Title = title,
                    Directory = title,
                    LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } },
                    Id = "tcm:0-0-0"
                };

                sgData = Client.Save(sgData, new ReadOptions()) as StructureGroupData;
                if (sgData == null)
                    return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static ComponentData GetComponent(MappingInfo mapping, string id)
        {
            if (String.IsNullOrEmpty(id))
                return null;

            if (!EnsureValidClient(mapping))
                return null;

            ComponentData component = ReadItem(mapping, id) as ComponentData;
            if (component == null)
                return null;

            if (component.BluePrintInfo.IsShared == true)
            {
                id = GetBluePrintTopTcmId(mapping, id);
                component = ReadItem(mapping, id) as ComponentData;
            }

            return component;
        }

        public static bool ExistsItem(MappingInfo mapping, string tcmContainer, string itemTitle)
        {
            if (String.IsNullOrEmpty(tcmContainer))
                return false;

            if (!EnsureValidClient(mapping))
                return false;

            OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData();
            return Client.GetList(tcmContainer, filter).Any(x => x.Title == itemTitle);
        }

        public static string GetItemTcmId(MappingInfo mapping, string tcmContainer, string itemTitle)
        {
            if (String.IsNullOrEmpty(tcmContainer))
                return String.Empty;

            if (!EnsureValidClient(mapping))
                return String.Empty;

            OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData();
            foreach (XElement element in Client.GetListXml(tcmContainer, filter).Nodes())
            {
                if (element.Attribute("Title").Value == itemTitle)
                    return element.Attribute("ID").Value;
            }

            return String.Empty;
        }

        public static IdentifiableObjectData ReadItem(MappingInfo mapping, string id)
        {
            if (!EnsureValidClient(mapping))
                return null;

            try
            {
                return Client.Read(id, null);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void SaveBinaryFromMultimediaComponent(MappingInfo mapping, string id, string targetDir)
        {
            if (!EnsureValidClient(mapping))
                return;

            ComponentData multimediaComponent = Client.Read(id, new ReadOptions()) as ComponentData;
            if (multimediaComponent == null)
                return;

            string path = Path.Combine(targetDir, Path.GetFileName(multimediaComponent.BinaryContent.Filename));

            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            EnsureValidStreamDownloadClient(mapping);

            using (Stream tempStream = StreamDownloadClient.DownloadBinaryContent(id))
            {
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    byte[] binaryContent = null;

                    if (multimediaComponent.BinaryContent.FileSize != -1)
                    {
                        var memoryStream = new MemoryStream();
                        tempStream.CopyTo(memoryStream);
                        binaryContent = memoryStream.ToArray();
                    }
                    if (binaryContent == null)
                        return;

                    fs.Write(binaryContent, 0, binaryContent.Length);
                }
            }
        }

        public static BinaryContentData GetBinaryData(MappingInfo mapping, string filePath)
        {
            EnsureValidStreamUploadClient(mapping);

            string title = Path.GetFileName(filePath);

            string tempLocation;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                tempLocation = StreamUploadClient.UploadBinaryContent(title, fs);
            }
            if (String.IsNullOrEmpty(tempLocation))
                return null;

            BinaryContentData binaryContent = new BinaryContentData();
            binaryContent.UploadFromFile = tempLocation;
            binaryContent.Filename = title;
            binaryContent.MultimediaType = new LinkToMultimediaTypeData { IdRef = GetMimeTypeId(mapping, filePath) };

            return binaryContent;
        }

        public static bool SaveMultimediaComponentFromBinary(MappingInfo mapping, string id, string filePath, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            if (String.IsNullOrEmpty(id))
                return false;

            if (!EnsureValidClient(mapping))
                return false;

            ComponentData multimediaComponent = ReadItem(mapping, id) as ComponentData;
            if (multimediaComponent == null)
                return false;

            if (multimediaComponent.BluePrintInfo.IsShared == true)
            {
                id = GetBluePrintTopTcmId(mapping, id);

                multimediaComponent = ReadItem(mapping, id) as ComponentData;
                if (multimediaComponent == null)
                    return false;
            }

            BinaryContentData binaryContent = GetBinaryData(mapping, filePath);
            if (binaryContent == null)
                return false;

            try
            {
                multimediaComponent = Client.CheckOut(id, true, new ReadOptions()) as ComponentData;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                return false;
            }

            if (multimediaComponent == null)
                return false;

            multimediaComponent.BinaryContent = binaryContent;
            multimediaComponent.ComponentType = ComponentType.Multimedia;

            try
            {
                multimediaComponent = Client.Update(multimediaComponent, new ReadOptions()) as ComponentData;
                if (multimediaComponent == null)
                    return false;

                Client.CheckIn(id, new ReadOptions());
                return true;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;

                if (multimediaComponent == null)
                    return false;

                Client.UndoCheckOut(multimediaComponent.Id, true, new ReadOptions());
                return false;
            }
        }

        public static bool SaveMultimediaComponentFromBinary(MappingInfo mapping, string filePath, string title, string tcmContainer, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            if (!File.Exists(filePath))
                return false;

            if (!EnsureValidClient(mapping))
                return false;

            if (String.IsNullOrEmpty(title))
                title = Path.GetFileName(filePath);

            if (ExistsItem(mapping, tcmContainer, title) || ExistsItem(mapping, tcmContainer, Path.GetFileNameWithoutExtension(filePath)) || ExistsItem(mapping, tcmContainer, Path.GetFileName(filePath)))
            {
                string id = GetItemTcmId(mapping, tcmContainer, title);
                if (String.IsNullOrEmpty(id))
                    id = GetItemTcmId(mapping, tcmContainer, Path.GetFileNameWithoutExtension(filePath));
                if (String.IsNullOrEmpty(id))
                    id = GetItemTcmId(mapping, tcmContainer, Path.GetFileName(filePath));

                if (String.IsNullOrEmpty(id))
                    return false;

                return SaveMultimediaComponentFromBinary(mapping, id, filePath, out stackTraceMessage);
            }

            try
            {
                BinaryContentData binaryContent = GetBinaryData(mapping, filePath);
                if (binaryContent == null)
                    return false;

                string tcmPublication = GetPublicationTcmId(tcmContainer);
                string schemaId = GetSchemas(mapping, tcmPublication).Single(x => x.Title == "Default Multimedia Schema").TcmId;

                ComponentData multimediaComponent = new ComponentData
                {
                    Title = title,
                    LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } },
                    Id = "tcm:0-0-0",
                    BinaryContent = binaryContent,
                    ComponentType = ComponentType.Multimedia,
                    Schema = new LinkToSchemaData { IdRef = schemaId },
                    IsBasedOnMandatorySchema = false,
                    IsBasedOnTridionWebSchema = true,
                    ApprovalStatus = new LinkToApprovalStatusData
                    {
                        IdRef = "tcm:0-0-0"
                    }
                };

                multimediaComponent = Client.Save(multimediaComponent, new ReadOptions()) as ComponentData;
                if (multimediaComponent == null)
                    return false;

                Client.CheckIn(multimediaComponent.Id, new ReadOptions());
                return true;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                return false;
            }
        }

        public static string GetMultimediaComponentFileExtension(MappingInfo mapping, string id)
        {
            ComponentData component = GetComponent(mapping, id);
            if (component == null || component.BinaryContent == null)
                return null;

            if (Extensions.Any(x => x.Value == component.BinaryContent.MimeType))
                return Extensions.Where(x => x.Value == component.BinaryContent.MimeType).Select(x => x.Key).First();
            
            return null;
        }

        #endregion

        #region Tridion hierarchy

        public static List<ItemInfo> GetFoldersByParentFolder(MappingInfo mapping, string tcmFolder)
        {
            if (!EnsureValidClient(mapping))
                return null;

            return Client.GetListXml(tcmFolder, new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.Folder } }).ToList(ItemType.Folder);
        }

        public static List<ItemInfo> GetTbbsByParentFolder(MappingInfo mapping, string tcmFolder)
        {
            if (!EnsureValidClient(mapping))
                return null;

            return Client.GetListXml(tcmFolder, new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.TemplateBuildingBlock } }).ToList(ItemType.TemplateBuildingBlock);
        }

        public static List<ItemInfo> GetStructureGroupsByParentStructureGroup(MappingInfo mapping, string tcmSG)
        {
            if (!EnsureValidClient(mapping))
                return null;

            return Client.GetListXml(tcmSG, new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.StructureGroup } }).ToList(ItemType.StructureGroup);
        }

        public static List<ItemInfo> GetFoldersByPublication(MappingInfo mapping, string tcmPublication)
        {
            if (!EnsureValidClient(mapping))
                return null;

            return Client.GetListXml(tcmPublication, new RepositoryItemsFilterData { ItemTypes = new[] { ItemType.Folder } }).ToList(ItemType.Folder);
        }

        public static List<ItemInfo> GetStructureGroupsByPublication(MappingInfo mapping, string tcmPublication)
        {
            if (!EnsureValidClient(mapping))
                return null;

            return Client.GetListXml(tcmPublication, new RepositoryItemsFilterData { ItemTypes = new[] { ItemType.StructureGroup } }).ToList(ItemType.StructureGroup);
        }

        public static List<ItemInfo> GetFoldersAndStructureGroupsByPublication(MappingInfo mapping, string tcmPublication)
        {
            List<ItemInfo> list = new List<ItemInfo>();
            List<ItemInfo> list1 = GetFoldersByPublication(mapping, tcmPublication);
            List<ItemInfo> list2 = GetStructureGroupsByPublication(mapping, tcmPublication);
            list.AddRange(list1);
            list.AddRange(list2);
            return list;
        }

        public static List<ItemInfo> GetPublications(MappingInfo mapping)
        {
            if (!EnsureValidClient(mapping))
                return null;

            return Client.GetSystemWideListXml(new PublicationsFilterData()).ToList(ItemType.Publication);
        }

        public static List<ItemInfo> GetSchemas(MappingInfo mapping, string tcmPublication)
        {
            if (!EnsureValidClient(mapping))
                return null;

            ItemInfo folder0 = GetFoldersByPublication(mapping, tcmPublication)[0];

            return Client.GetListXml(folder0.TcmId, new OrganizationalItemItemsFilterData { Recursive = true, ItemTypes = new[] { ItemType.Schema } }).ToList(ItemType.Schema);
        }

        public static List<string> GetSchemaDateFields(MappingInfo mapping, string tcmSchema)
        {
            if (!EnsureValidClient(mapping))
                return null;

            SchemaFieldsData schemaFieldsData = Client.ReadSchemaFields(tcmSchema, false, null);
            var fields = schemaFieldsData.Fields.Where(x => x is DateFieldDefinitionData).ToList();
            var metadataFields = schemaFieldsData.MetadataFields.Where(x => x is DateFieldDefinitionData).ToList();
            List<string> res = new List<string>();
            res.AddRange(fields.Cast<DateFieldDefinitionData>().Select(x => x.Name).ToList());
            res.AddRange(metadataFields.Cast<DateFieldDefinitionData>().Select(x => x.Name + " - metadata field").ToList());
            return res;
        }

        public static List<ItemInfo> GetComponents(MappingInfo mapping, string tcmSchema)
        {
            if (!EnsureValidClient(mapping))
                return null;

            return Client.GetListXml(tcmSchema, new UsingItemsFilterData { ItemTypes = new[] { ItemType.Component } }).ToList(ItemType.Component);
        }

        public static List<XElement> GetComponentHistory(MappingInfo mapping, string tcmId)
        {
            if (!EnsureValidClient(mapping))
                return null;

            VersionsFilterData versionsFilter = new VersionsFilterData();
            XElement listOfVersions = Client.GetListXml(tcmId, versionsFilter);
            return listOfVersions.Descendants().ToList();
        }

        public static List<ItemInfo> GetPages(MappingInfo mapping, string tcmComponent)
        {
            if (!EnsureValidClient(mapping))
                return null;

            return Client.GetListXml(tcmComponent, new UsingItemsFilterData { ItemTypes = new[] { ItemType.Page } }).ToList(ItemType.Page);
        }

        public static string GetWebDav(this RepositoryLocalObjectData item)
        {
            string webDav = System.Web.HttpUtility.UrlDecode(item.LocationInfo.WebDavUrl.Replace("/webdav/", string.Empty));
            if (string.IsNullOrEmpty(webDav))
                return string.Empty;

            string fileExtension = Path.GetExtension(webDav);

            return string.IsNullOrEmpty(fileExtension) ? webDav : webDav.Replace(fileExtension, string.Empty);
        }

        private static List<string> GetUsingItems(string tcmItem)
        {
            UsingItemsFilterData filter = new UsingItemsFilterData();
            filter.IncludedVersions = VersionCondition.AllVersions;
            filter.BaseColumns = ListBaseColumns.Id;
            List<string> items = Client.GetListXml(tcmItem, filter).ToList().Select(x => x.TcmId).ToList();
            return items;
        }

        private static List<string> GetUsingCurrentItems(string tcmItem)
        {
            UsingItemsFilterData filter = new UsingItemsFilterData();
            filter.IncludedVersions = VersionCondition.OnlyLatestVersions;
            filter.BaseColumns = ListBaseColumns.Id;
            List<string> items = Client.GetListXml(tcmItem, filter).ToList().Select(x => x.TcmId).ToList();
            return items;
        }

        private static List<string> GetUsedItems(string tcmItem)
        {
            UsedItemsFilterData filter = new UsedItemsFilterData();
            filter.BaseColumns = ListBaseColumns.Id;
            List<string> items = Client.GetListXml(tcmItem, filter).ToList().Select(x => x.TcmId).ToList();
            return items;
        }

        public static List<HistoryItemInfo> GetItemHistory(MappingInfo mapping, string tcmItem)
        {
            if (!EnsureValidClient(mapping))
                return null;

            return GetItemHistory(tcmItem);
        }

        public static List<HistoryItemInfo> GetItemHistory(string tcmItem)
        {
            VersionsFilterData versionsFilter = new VersionsFilterData();
            XElement listOfVersions = Client.GetListXml(tcmItem, versionsFilter);

            List<HistoryItemInfo> res = new List<HistoryItemInfo>();

            if (listOfVersions != null && listOfVersions.HasElements)
            {
                foreach (XElement element in listOfVersions.Descendants())
                {
                    HistoryItemInfo item = new HistoryItemInfo();
                    item.TcmId = element.Attribute("ID").Value;
                    item.ItemType = element.Attributes().Any(x => x.Name == "Type") ? (ItemType)Int32.Parse(element.Attribute("Type").Value) : GetItemType(item.TcmId);
                    item.Title = element.Attributes().Any(x => x.Name == "Title") ? element.Attribute("Title").Value : item.TcmId;
                    item.Version = int.Parse(element.Attribute("Version").Value.Replace("v", ""));
                    item.Modified = DateTime.Parse(element.Attribute("Modified").Value);

                    res.Add(item);
                }
            }

            res.Last().Current = true;

            return res;
        }

        public static string GetItemFolder(MappingInfo mapping, string tcmItem)
        {
            if (!EnsureValidClient(mapping))
                return null;

            return GetItemFolder(tcmItem);
        }

        private static string GetItemFolder(string tcmItem)
        {
            RepositoryLocalObjectData item = Client.Read(tcmItem, new ReadOptions()) as RepositoryLocalObjectData;
            if (item == null)
                return string.Empty;

            return item.LocationInfo.OrganizationalItem.IdRef;
        }

        public static List<ItemInfo> ToList(this XElement xml, ItemType itemType)
        {
            List<ItemInfo> res = new List<ItemInfo>();
            if (xml != null && xml.HasElements)
            {
                foreach (XElement element in xml.Nodes())
                {
                    ItemInfo item = new ItemInfo();
                    item.TcmId = element.Attribute("ID").Value;
                    item.ItemType = itemType;
                    item.Title = element.Attributes().Any(x => x.Name == "Title") ? element.Attribute("Title").Value : item.TcmId;
                    item.MimeType = element.Attributes().Any(x => x.Name == "MIMEType") ? element.Attribute("MIMEType").Value : null;
                    item.FromPub = element.Attributes().Any(x => x.Name == "FromPub") ? element.Attribute("FromPub").Value : null;
                    item.IsPublished = element.Attributes().Any(x => x.Name == "Icon") && element.Attribute("Icon").Value.EndsWith("P1");
                    res.Add(item);
                }
            }
            return res;
        }

        public static List<ItemInfo> ToList(this XElement xml)
        {
            List<ItemInfo> res = new List<ItemInfo>();
            if (xml != null && xml.HasElements)
            {
                foreach (XElement element in xml.Nodes())
                {
                    ItemInfo item = new ItemInfo();
                    item.TcmId = element.Attribute("ID").Value;
                    item.ItemType = element.Attributes().Any(x => x.Name == "Type") ? (ItemType)Int32.Parse(element.Attribute("Type").Value) : GetItemType(item.TcmId);
                    item.Title = element.Attributes().Any(x => x.Name == "Title") ? element.Attribute("Title").Value : item.TcmId;
                    item.MimeType = element.Attributes().Any(x => x.Name == "MIMEType") ? element.Attribute("MIMEType").Value : null;
                    item.FromPub = element.Attributes().Any(x => x.Name == "FromPub") ? element.Attribute("FromPub").Value : null;
                    item.IsPublished = element.Attributes().Any(x => x.Name == "Icon") && element.Attribute("Icon").Value.EndsWith("P1");
                    res.Add(item);
                }
            }
            return res;
        }

        public static ItemType GetItemType(string tcmItem)
        {
            string[] arr = tcmItem.Replace("tcm:", string.Empty).Split('-');
            if (arr.Length == 2) return ItemType.Component;

            return (ItemType)Int32.Parse(arr[2]);
        }

        public static List<ItemInfo> MakeExpandable(this List<ItemInfo> list)
        {
            foreach (ItemInfo item in list)
            {
                if (item.ChildItems == null)
                    item.ChildItems = new List<ItemInfo> {new ItemInfo {Title = "Loading..."}};
            }
            return list;
        }

        public static List<ItemInfo> Expand(this List<ItemInfo> list, MappingInfo mapping, TridionSelectorMode tridionSelectorMode, List<string> tcmIdPath, string selectedTcmId)
        {
            if (tcmIdPath == null || String.IsNullOrEmpty(selectedTcmId))
                return list;

            foreach (ItemInfo item in list)
            {
                if (tcmIdPath.Any(x => x == item.TcmId))
                {
                    item.IsExpanded = true;
                    item.IsSelected = item.TcmId == selectedTcmId;

                    if (item.IsSelected)
                        continue;

                    if (String.IsNullOrEmpty(item.TcmId))
                        continue;

                    item.ChildItems = null;
                    if (item.ItemType == ItemType.Publication)
                    {
                        if (tridionSelectorMode == TridionSelectorMode.Folder)
                        {
                            item.ChildItems = GetFoldersByPublication(mapping, item.TcmId);
                        }
                        if (tridionSelectorMode == TridionSelectorMode.StructureGroup)
                        {
                            item.ChildItems = GetStructureGroupsByPublication(mapping, item.TcmId);
                        }
                        if (tridionSelectorMode == TridionSelectorMode.FolderAndStructureGroup)
                        {
                            item.ChildItems = GetFoldersAndStructureGroupsByPublication(mapping, item.TcmId);
                        }
                    }
                    if (item.ItemType == ItemType.Folder)
                    {
                        item.ChildItems = GetFoldersByParentFolder(mapping, item.TcmId);
                    }
                    if (item.ItemType == ItemType.StructureGroup)
                    {
                        item.ChildItems = GetStructureGroupsByParentStructureGroup(mapping, item.TcmId);
                    }

                    if (item.ChildItems != null && item.ChildItems.Count > 0)
                    {
                        item.ChildItems.SetParent(item);
                    }

                    if (item.ChildItems != null && item.ChildItems.Count > 0)
                    {
                        item.ChildItems.Expand(mapping, tridionSelectorMode, tcmIdPath, selectedTcmId);
                    }
                }
                else
                {
                    item.ChildItems = new List<ItemInfo> { new ItemInfo { Title = "Loading..." } };
                }
            }
            return list;
        }

        public static List<ItemInfo> SetParent(this List<ItemInfo> list, ItemInfo parent)
        {
            foreach (ItemInfo item in list)
            {
                item.Parent = parent;
            }
            return list;
        }

        public static string GetMimeTypeId(MappingInfo mapping, string filePath)
        {
            if (!EnsureValidClient(mapping))
                return String.Empty;

            List<MultimediaTypeData> allMimeTypes = Client.GetSystemWideList(new MultimediaTypesFilterData()).Cast<MultimediaTypeData>().ToList();
            foreach (MultimediaTypeData mt in allMimeTypes)
            {
                foreach (string ext in mt.FileExtensions)
                {
                    if (Path.GetExtension(filePath).ToLower().Replace(".", "") == ext.ToLower().Replace(".", ""))
                        return mt.Id;
                }
            }
            return String.Empty;
        }

        public static string GetPublicationTcmId(string id)
        {
            return "tcm:0-" + id.Replace("tcm:", "").Split('-')[0] + "-1";
        }

        public static string GetBluePrintItemTcmId(string id, string publicationId)
        {
            return "tcm:" + publicationId.Split('-')[1] + "-" + id.Split('-')[1] + "-" + id.Split('-')[2];
        }

        public static List<ItemInfo> FindCheckedOutItems(MappingInfo mapping)
        {
            if (!EnsureValidClient(mapping))
                return null;

            return Client.GetSystemWideListXml(new RepositoryLocalObjectsFilterData()).ToList();
        }

        public static bool IsCheckedOut(MappingInfo mapping, string id)
        {
            return FindCheckedOutItems(mapping).Any(x => x.TcmId == id);
        }

        public static List<ItemInfo> GetPublications(MappingInfo mapping, string filterItemId)
        {
            if (!EnsureValidClient(mapping))
                return null;

            List<ItemInfo> publications = GetPublications(mapping);

            var allowedPublications = Client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = filterItemId } }).Cast<BluePrintNodeData>().Where(x => x.Item != null).Select(x => GetPublicationTcmId(x.Item.Id)).ToList();

            return publications.Where(x => allowedPublications.Any(y => y == x.TcmId)).ToList();
        }

        #endregion

        #region VS hierarchy

        public static IEnumerable<ProjectFolderInfo> GetFileTree(ProjectFolderInfo projectFolder, string rootPath)
        {
            AddChildItems(projectFolder, rootPath);

            ProjectFolderInfo rootFolder = GetRootTree(projectFolder, rootPath);

            return new List<ProjectFolderInfo> { rootFolder };
        }

        public static void AddChildItems(ProjectFolderInfo projectFolder, string rootPath)
        {
            if (projectFolder == null)
                return;

            if (String.IsNullOrEmpty(projectFolder.FullPath))
                return;

            if (!Directory.Exists(projectFolder.FullPath))
                Directory.CreateDirectory(projectFolder.FullPath);

            ProjectFolderInfo topFolder = projectFolder.GetTopFolder();
            ProjectFolderRole role = topFolder.ProjectFolderRole;
            string[] extensions = role == TridionVSRazorExtension.ProjectFolderRole.Binary ? Extensions.Keys.ToArray() : new[] { "*.cshtml" };
            string[] directories = Directory.GetDirectories(projectFolder.FullPath);
            string[] files = GetFiles(projectFolder.FullPath, extensions);

            if (directories.Length == 0 && files.Length == 0)
            {
                projectFolder.ChildItems = null;
                return;
            }

            List<ProjectItemInfo> newChildItems = new List<ProjectItemInfo>();

            foreach (string dir in directories)
            {
                ProjectFolderInfo childFolder = null;

                if (projectFolder.ChildItems != null)
                {
                    childFolder = projectFolder.ChildItems.FirstOrDefault(x => x.IsFolder && x.FullPath == dir) as ProjectFolderInfo;
                }

                if (childFolder == null)
                {
                    childFolder = new ProjectFolderInfo { RootPath = rootPath, Path = dir.Replace(rootPath, "").Trim('\\'), Checked = false };
                }

                childFolder.Parent = projectFolder;

                AddChildItems(childFolder, rootPath);

                newChildItems.Add(childFolder);
            }

            foreach (string file in files)
            {
                ProjectFileInfo childFile = null;

                if (projectFolder.ChildItems != null)
                {
                    childFile = projectFolder.ChildItems.FirstOrDefault(x => x.IsFile && x.FullPath == file) as ProjectFileInfo;
                }

                if (childFile == null)
                {
                    childFile = new ProjectFileInfo { RootPath = rootPath, Path = file.Replace(rootPath, "").Trim('\\'), Checked = false };
                }

                childFile.Parent = projectFolder;

                newChildItems.Add(childFile);
            }

            projectFolder.ChildItems = newChildItems.Count > 0 ? newChildItems : null;
        }

        public static ProjectFolderInfo GetRootTree(ProjectFolderInfo projectFolder, string fullPath)
        {
            if (projectFolder == null)
                return null;

            if (!Directory.Exists(fullPath))
                return null;

            if (!ExistFile(projectFolder, fullPath) && projectFolder.FullPath != fullPath)
                return null;

            if (projectFolder.FullPath == fullPath)
            {
                projectFolder.IsSelected = true;
                projectFolder.Expand();
                return projectFolder;
            }

            string rootPath = projectFolder.RootPath;

            ProjectFolderInfo parentFolder = new ProjectFolderInfo { RootPath = rootPath, Path = fullPath.Replace(rootPath, "").Trim('\\'), Checked = false };
            parentFolder.ChildItems = new List<ProjectItemInfo>();

            foreach (string childFolderPath in Directory.GetDirectories(fullPath))
            {
                ProjectFolderInfo childFolder = GetRootTree(projectFolder, childFolderPath);
                if (childFolder != null)
                {
                    childFolder.Parent = parentFolder;
                    parentFolder.ChildItems.Add(childFolder);
                }
            }

            foreach (string childFilePath in GetFiles(fullPath, projectFolder.ProjectFolderRole == TridionVSRazorExtension.ProjectFolderRole.Binary ? Extensions.Keys.ToArray() : new[] { "*.cshtml" }))
            {
                ProjectFileInfo childFile = new ProjectFileInfo { RootPath = rootPath, Path = childFilePath.Replace(rootPath, "").Trim('\\'), Checked = false };
                childFile.Parent = parentFolder;
                parentFolder.ChildItems.Add(childFile);
            }

            if (parentFolder.ChildItems.Count == 0)
                parentFolder.ChildItems = null;

            return parentFolder;
        }

        public static ProjectFolderInfo GetSelectedFolderFromTree(IEnumerable<ProjectFolderInfo> tree)
        {
            foreach (ProjectFolderInfo projectFolder in tree)
            {
                if (projectFolder == null)
                    continue;

                if (projectFolder.Checked != false)
                {
                    return projectFolder;
                }

                if (projectFolder.ChildItems == null)
                    continue;

                var res = GetSelectedFolderFromTree(projectFolder.ChildItems.Where(x => x.IsFolder).Cast<ProjectFolderInfo>().ToList());
                if (res != null)
                    return res;
            }
            return null;
        }

        public static bool ExistFile(ProjectFolderInfo projectFolder, string dir, string extension)
        {
            return Directory.GetFiles(dir, "*" + extension).Any() || Directory.GetDirectories(dir).Any(x => ExistFile(projectFolder, x, extension));
        }

        public static bool ExistFile(ProjectFolderInfo projectFolder, string dir)
        {
            if (projectFolder.ProjectFolderRole == TridionVSRazorExtension.ProjectFolderRole.Binary)
            {
                return Extensions.Keys.Select(extension => ExistFile(projectFolder, dir, extension)).Any(x => x);
            }

            return ExistFile(projectFolder, dir, "*.cshtml");
        }

        public static string[] GetFiles(string dir, string[] allowedExtensions)
        {
            if (String.IsNullOrEmpty(dir) || allowedExtensions == null)
                return new string[] {};

            List<string> res = new List<string>();
            foreach (string extension in allowedExtensions)
            {
                res.AddRange(Directory.GetFiles(dir, "*" + extension));
            }

            res.Sort();

            return res.ToArray();
        }

        public static void Expand(this ProjectFolderInfo folder)
        {
            folder.IsExpanded = true;
            if (folder.Parent != null)
                folder.Parent.Expand();
        }

        #endregion

        #region Serialization helpers

        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(Configuration));

        public static void SaveConfiguration(string rootPath, string name, Configuration configuration)
        {
            if (configuration == null)
                return;

            Configuration clearedConfiguration = configuration.ClearConfiguration();
            if (clearedConfiguration == null)
                return;

            string path = Path.Combine(rootPath, name);

            if (File.Exists(path))
            {
                string xml;
                XmlWriterSettings settings = new XmlWriterSettings { Indent = true, ConformanceLevel = ConformanceLevel.Auto, OmitXmlDeclaration = false };
                using (MemoryStream ms = new MemoryStream())
                {
                    using (var writer = XmlWriter.Create(ms, settings))
                    {
                        Serializer.Serialize(writer, clearedConfiguration);
                    }
                    xml = Encoding.UTF8.GetString(ms.ToArray());
                }

                //check-out and replace code
                //SetCode(path, xml);

                //replace code
                File.WriteAllText(path, xml);
            }
            else
            {
                XmlWriterSettings settings = new XmlWriterSettings { Indent = true, ConformanceLevel = ConformanceLevel.Auto, OmitXmlDeclaration = false };
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    using (var writer = XmlWriter.Create(fs, settings))
                    {
                        Serializer.Serialize(writer, clearedConfiguration);
                    }
                }
                ProjectItem projectItem = Project.ProjectItems.AddFromFile(path);
                Marshal.ReleaseComObject(projectItem);
            }
        }

        public static Configuration ClearConfiguration(this Configuration configuration)
        {
            Configuration clearedConfiguration = new Configuration();
            clearedConfiguration.AddRange(configuration.Select(mapping => mapping.ClearMapping()));
            return clearedConfiguration;
        }

        public static MappingInfo ClearMapping(this MappingInfo mapping)
        {
            MappingInfo clearedMapping = new MappingInfo();
            clearedMapping.Name = mapping.Name;
            clearedMapping.Host = mapping.Host;
            clearedMapping.Username = mapping.Username;
            clearedMapping.Password = mapping.Password;
            clearedMapping.TimeZoneId = mapping.TimeZoneId;
            clearedMapping.TridionFolders = mapping.TridionFolders;

            if (mapping.ProjectFolders != null && mapping.ProjectFolders.Count > 0)
            {
                clearedMapping.ProjectFolders = new List<ProjectFolderInfo>();
                foreach (ProjectFolderInfo projectFolder in mapping.ProjectFolders)
                {
                    ProjectFolderInfo clearedProjectFolder = projectFolder.ClearProjectFolder(false);
                    if (clearedProjectFolder != null)
                        clearedMapping.ProjectFolders.Add(clearedProjectFolder);
                }
            }

            return clearedMapping;
        }

        public static ProjectFolderInfo ClearProjectFolder(this ProjectFolderInfo projectFolder, bool checkChecked)
        {
            if (projectFolder == null)
                return null;

            if (checkChecked && projectFolder.Checked == false)
                return null;

            ProjectFolderInfo clearedProjectFolder = new ProjectFolderInfo();
            clearedProjectFolder.Path = projectFolder.Path;
            clearedProjectFolder.Checked = projectFolder.Checked;
            clearedProjectFolder.SyncTemplate = projectFolder.SyncTemplate;
            clearedProjectFolder.TemplateFormat = projectFolder.TemplateFormat;
            clearedProjectFolder.ProjectFolderRole = projectFolder.ProjectFolderRole;
            clearedProjectFolder.TcmId = projectFolder.TcmId;

            if (projectFolder.ChildItems != null && projectFolder.ChildItems.Count > 0)
            {
                clearedProjectFolder.ChildItems = new List<ProjectItemInfo>();
                foreach (ProjectItemInfo childItem in projectFolder.ChildItems)
                {
                    if (childItem.IsFolder)
                    {
                        ProjectFolderInfo clearedChildProjectFolder = ((ProjectFolderInfo)childItem).ClearProjectFolder(true);
                        if (clearedChildProjectFolder != null)
                            clearedProjectFolder.ChildItems.Add(clearedChildProjectFolder);
                    }
                    if (childItem.IsFile)
                    {
                        if (childItem.Checked == true)
                        {
                            clearedProjectFolder.ChildItems.Add(childItem);
                        }
                    }
                }
                if (clearedProjectFolder.ChildItems.Count == 0)
                    clearedProjectFolder.ChildItems = null;
            }

            return clearedProjectFolder;
        }

        public static Configuration GetConfiguration(string rootPath, string name)
        {
            string path = Path.Combine(rootPath, name);
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Configuration configuration = (Configuration)Serializer.Deserialize(fs);

                //set root path property to chind items
                foreach (MappingInfo mapping in configuration)
                {
                    foreach (ProjectFolderInfo folder in mapping.ProjectFolders)
                    {
                        SetRootPath(folder, rootPath);
                    }
                }

                //merge with file system
                foreach (MappingInfo mapping in configuration)
                {
                    foreach (ProjectFolderInfo projectFolder in mapping.ProjectFolders)
                    {
                        AddChildItems(projectFolder, rootPath);
                    }
                }

                return configuration;
            }
        }

        public static void SetRootPath(ProjectFolderInfo folder, string rootPath)
        {
            folder.RootPath = rootPath;
            if (folder.ChildItems != null)
            {
                foreach (ProjectItemInfo childItem in folder.ChildItems)
                {
                    childItem.Parent = folder;
                    if (childItem.IsFile)
                    {
                        childItem.RootPath = rootPath;
                    }
                    if (childItem.IsFolder)
                    {
                        SetRootPath((ProjectFolderInfo) childItem, rootPath);
                    }
                }
            }
        }

        public static Configuration GetDefault(string rootPath)
        {
            Configuration res = new Configuration();
            res.DefaultConfiguration = "Default";

            MappingInfo mapping = GetDefaultMapping(rootPath, "Default");
            res.Add(mapping);

            return res;
        }

        public static MappingInfo GetDefaultMapping(string rootPath, string name)
        {
            MappingInfo mapping = new MappingInfo();
            mapping.Name = name;
            mapping.Host = "localhost";
            mapping.Username = "";
            mapping.Password = "";

            mapping.TridionFolders = new List<TridionFolderInfo>();
            mapping.TridionFolders.Add(new TridionFolderInfo { TridionRole = TridionRole.PageLayoutContainer, ScanForItems = true});
            mapping.TridionFolders.Add(new TridionFolderInfo { TridionRole = TridionRole.ComponentLayoutContainer, ScanForItems = true });
            mapping.TridionFolders.Add(new TridionFolderInfo { TridionRole = TridionRole.PageTemplateContainer });
            mapping.TridionFolders.Add(new TridionFolderInfo { TridionRole = TridionRole.ComponentTemplateContainer });

            mapping.ProjectFolders = new List<ProjectFolderInfo>();
            mapping.ProjectFolders.Add(new ProjectFolderInfo
            {
                ProjectFolderRole = TridionVSRazorExtension.ProjectFolderRole.PageLayout,
                Checked = true,
                RootPath = rootPath,
                Path = "Views\\PageLayouts",
                TemplateFormat =
                    "<CompoundTemplate xmlns=\"http://www.tridion.com/ContentManager/5.3/CompoundTemplate\"><TemplateInvocation><Template xlink:href=\"{0}\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xlink:title=\"{1}\" /><TemplateParameters></TemplateParameters></TemplateInvocation></CompoundTemplate>",
                SyncTemplate = true
            });
            mapping.ProjectFolders.Add(new ProjectFolderInfo
            {
                ProjectFolderRole = TridionVSRazorExtension.ProjectFolderRole.ComponentLayout,
                Checked = true,
                RootPath = rootPath,
                Path = "Views\\ComponentLayouts",
                TemplateFormat =
                    "<CompoundTemplate xmlns=\"http://www.tridion.com/ContentManager/5.3/CompoundTemplate\"><TemplateInvocation><Template xlink:href=\"{0}\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xlink:title=\"{1}\" /><TemplateParameters></TemplateParameters></TemplateInvocation></CompoundTemplate>",
                SyncTemplate = true
            });
            return mapping;
        }

        #endregion

        #region Isolated storage

        public static string GetFromIsolatedStorage(string key)
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain, typeof(System.Security.Policy.Url), typeof(System.Security.Policy.Url)))
            {
                if (!isf.FileExists(key + ".txt"))
                    return String.Empty;

                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(key + ".txt", FileMode.Open, isf))
                {
                    using (StreamReader sr = new StreamReader(isfs, System.Text.Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }

        public static void SaveToIsolatedStorage(string key, string value)
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain, typeof(System.Security.Policy.Url), typeof(System.Security.Policy.Url)))
            {
                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(key + ".txt", FileMode.Create, isf))
                {
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(value);
                    isfs.Write(data, 0, data.Length);
                }
            }
        }

        #endregion

        #region Text helpers

        public static string TransformRazor2Mediator(string code)
        {
            //if (IsTridionCM())
                code = code.Replace("@inherits Tridion.Extensions.Mediators.Razor.TridionRazorTemplate", "");

            return code.RemoveSpaces();
        }

        public static string TransformMediator2Razor(string code)
        {
            //if (IsTridionCM())
                code = "@inherits Tridion.Extensions.Mediators.Razor.TridionRazorTemplate\r\n\r\n" + code;

            return code.RemoveSpaces();
        }

        public static string RemoveSpaces(this string str)
        {
            string res = Regex.Replace(str, "\\s+\\r\\n", "\r\n");
            res = Regex.Replace(res, "\\t", "    ");
            return res.Trim();
        }

        public static string GetDiffHtml(string text1, string text2)
        {
            diff_match_patch dmp = new diff_match_patch();
            dmp.Diff_Timeout = 0;

            var diffs = dmp.diff_main(text1, text2);
            var html = dmp.diff_prettyHtml(diffs);

            return html;
        }

        public static string CutPath(this string path, string separator, int maxLength)
        {
            if (path == null || path.Length <= maxLength)
                return path;

            var list = path.Split(new[] { separator[0] });
            int itemMaxLength = maxLength / list.Length;

            return string.Join(separator, list.Select(item => item.Cut(itemMaxLength)).ToList());
        }

        public static string CutPath(this string path, string separator, int maxLength, bool fullLastItem)
        {
            if (path == null || path.Length <= maxLength)
                return path;

            if (!fullLastItem)
                return path.CutPath(separator, maxLength);

            string lastItem = path.Substring(path.LastIndexOf(separator, StringComparison.Ordinal));

            if (lastItem.Length > maxLength)
                return path.CutPath(separator, maxLength);

            return path.Substring(0, path.LastIndexOf(separator, StringComparison.Ordinal)).CutPath(separator, maxLength - lastItem.Length) + lastItem;
        }

        public static string Cut(this string str, int maxLength)
        {
            if (maxLength < 5)
                maxLength = 5;

            if (str.Length > maxLength)
            {
                return str.Substring(0, maxLength - 2) + "..";

            }
            return str;
        }

        public static string PrettyXml(this string xml)
        {
            try
            {
                return XElement.Parse(xml).ToString();
            }
            catch (Exception)
            {
                return xml;
            }
        }

        public static string PlainXml(this string xml)
        {
            try
            {
                return Regex.Replace(xml, "\\s+", " ").Replace("> <", "><");
            }
            catch (Exception)
            {
                return xml;
            }
        }

        public static string GetDomainName(this string url)
        {
            if (!url.Contains(Uri.SchemeDelimiter))
            {
                url = string.Concat(Uri.UriSchemeHttp, Uri.SchemeDelimiter, url);
            }
            Uri uri = new Uri(url);
            return uri.Host;
        }

        #endregion

        #region Syncronization helpers

        public static void SyncRazorLayoutTbb(MappingInfo mapping, string tcmContainer, ProjectFileInfo file, ProjectFolderRole role, string serverTimeZoneId)
        {
            string path = file.FullPath;
            if (String.IsNullOrEmpty(path))
                return;

            if (!File.Exists(path))
            {
                //remove non-existing file from mapping
                foreach (ProjectFolderInfo folder in mapping.ProjectFolders)
                {
                    DeleteFileFromMapping(folder, path);
                }
                return;
            }

            IncludeProjectItem(path);

            string title;
            if (String.IsNullOrEmpty(file.Title))
            {
                title = Path.GetFileNameWithoutExtension(path);
                file.Title = title;
            }
            else
            {
                title = file.Title;
            }

            string id;
            if (String.IsNullOrEmpty(file.TcmId))
            {
                id = GetItemTcmId(mapping, tcmContainer, title);
                file.TcmId = id;
            }
            else
            {
                id = file.TcmId;
            }

            string fileContent = TransformRazor2Mediator(File.ReadAllText(path));

            bool updated = false;

            if (String.IsNullOrEmpty(id))
            {
                TridionDestinationDialogWindow dialog = new TridionDestinationDialogWindow();
                dialog.Mapping = mapping;
                dialog.FilterItemTcmId = tcmContainer;
                dialog.PublicationTcmId = GetPublicationTcmId(tcmContainer);
                dialog.LayoutTitle = title;
                dialog.TemplateTitle = String.IsNullOrEmpty(file.TemplateTitle) ? title : file.TemplateTitle;
                bool res = dialog.ShowDialog() == true;
                if (res)
                {
                    file.Title = dialog.LayoutTitle;
                    file.TemplateTitle = dialog.TemplateTitle;
                    tcmContainer = GetBluePrintItemTcmId(tcmContainer, dialog.PublicationTcmId);
                    string stackTraceMessage;
                    updated = SaveRazorLayoutTbb(mapping, file.Title, fileContent, tcmContainer, out stackTraceMessage);
                    if (updated)
                    {
                        id = GetItemTcmId(mapping, tcmContainer, file.Title);
                        if (String.IsNullOrEmpty(id))
                        {
                            WriteErrorLog(file.Path + " - Item is broken after updating");
                            return;
                        }
                        file.TcmId = id;
                    }
                    else
                    {
                        WriteErrorLog(file.Path + " - Creating failed", stackTraceMessage);
                        return;
                    }
                }
            }

            TemplateBuildingBlockData item = ReadItem(mapping, id) as TemplateBuildingBlockData;

            if (item == null || item.VersionInfo.RevisionDate == null)
            {
                WriteErrorLog(string.Format("Item {0} does not exist", id));
                return;
            }

            FileInfo fi = new FileInfo(path);
            DateTime fileDate = fi.LastWriteTime;
            DateTime tridionDate = (DateTime) item.VersionInfo.RevisionDate;
            DateTime tridionLocalDate = tridionDate.GetLocalTime(serverTimeZoneId);

            if (updated)
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.SetLastWriteTime(path, tridionLocalDate);
                WriteSuccessLog(file.Path + " - Saved to Tridion CM successfully");
                return;
            }

            if (fileDate == tridionLocalDate)
                return;

            string tridionContent = item.Content;
            file.Title = item.Title;

            if (Equals(fileContent, tridionContent))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.SetLastWriteTime(path, tridionLocalDate);
                WriteSuccessLog(file.Path + " - Date of project item is updated to " + tridionLocalDate);
            }
            else
            {
                string user = item.VersionInfo is FullVersionInfo && ((FullVersionInfo)item.VersionInfo).Revisor != null ? ((FullVersionInfo)item.VersionInfo).Revisor.Title : "Unknown";

                DiffDialogWindow dialog = new DiffDialogWindow();
                if (fileDate > tridionLocalDate)
                {
                    dialog.StartItemInfo = string.Format("Tridion Item: {0} ({1}), {2}, {3}", item.Title, item.Id, tridionLocalDate, user);
                    dialog.EndItemInfo = string.Format("VS File: {0}, {1}", Path.GetFileName(path), fileDate);
                    dialog.StartItemText = tridionContent;
                    dialog.EndItemText = fileContent;
                    dialog.SyncState = SyncState.VS2Tridion;
                    dialog.Tridion2VSEnabled = true;
                    dialog.VS2TridionEnabled = !IsCheckedOut(mapping, item.Id);
                }
                else
                {
                    dialog.StartItemInfo = string.Format("VS File: {0}, {1}", Path.GetFileName(path), fileDate);
                    dialog.EndItemInfo = string.Format("Tridion Item: {0} ({1}), {2}, {3}", item.Title, item.Id, tridionLocalDate, user);
                    dialog.StartItemText = fileContent;
                    dialog.EndItemText = tridionContent;
                    dialog.SyncState = SyncState.Tridion2VS;
                    dialog.Tridion2VSEnabled = true;
                    dialog.VS2TridionEnabled = true;
                }

                bool res = dialog.ShowDialog() == true;
                if (res)
                {
                    if (dialog.SyncState == SyncState.VS2Tridion)
                    {
                        string stackTraceMessage;
                        updated = SaveRazorLayoutTbb(mapping, id, fileContent, out stackTraceMessage);
                        if (updated)
                        {
                            item = ReadItem(mapping, id) as TemplateBuildingBlockData;
                            if (item == null || item.VersionInfo.RevisionDate == null)
                            {
                                WriteErrorLog(string.Format("Item {0} does not exist", id));
                            }
                            else
                            {
                                tridionDate = (DateTime)item.VersionInfo.RevisionDate;
                                tridionLocalDate = tridionDate.GetLocalTime(serverTimeZoneId);
                                File.SetAttributes(path, FileAttributes.Normal);
                                File.SetLastWriteTime(path, tridionLocalDate);
                                WriteSuccessLog(file.Path + " - Saved to Tridion CM successfully");
                            }
                        }
                        else
                        {
                            WriteErrorLog(file.Path + " - Updating failed", stackTraceMessage);
                        }
                    }
                    else if (dialog.SyncState == SyncState.Tridion2VS)
                    {
                        SaveVSItem(path, tridionContent);
                        File.SetAttributes(path, FileAttributes.Normal);
                        File.SetLastWriteTime(path, tridionLocalDate);
                        WriteSuccessLog(file.Path + " - Saved to Visual Studio successfully");
                    }
                }
            }
        }

        public static DateTime GetLocalTime(this DateTime serverDate, string serverTimeZoneId)
        {
            if (String.IsNullOrEmpty(serverTimeZoneId))
                serverTimeZoneId = TimeZoneInfo.Local.Id;

            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(serverDate, serverTimeZoneId, TimeZoneInfo.Local.Id);
        }

        private static void SaveVSItem(string path, string tridionContent)
        {
            string directory = Path.GetDirectoryName(path);
            if (!String.IsNullOrEmpty(directory))
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    ProjectItem dir = Project.ProjectItems.AddFromDirectory(directory);
                    Marshal.ReleaseComObject(dir);
                }
            }

            tridionContent = TransformMediator2Razor(tridionContent);

            //check-out and replace code
            //SetCode(path, tridionContent);

            //write / replace code
            File.WriteAllText(path, tridionContent);

            IncludeProjectItem(path);
        }

        private static void IncludeProjectItem(string path)
        {
            ProjectItem item = Project.ProjectItems.AddFromFile(path);

            //BuildAction == None
            Property prop = item.Properties.Item("BuildAction");
            if (prop != null)
                prop.Value = 0;

            Marshal.ReleaseComObject(item);
        }

        public static void CreateVSFolder(string fullPath)
        {
            if (!String.IsNullOrEmpty(fullPath))
            {
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    ProjectItem dir = Project.ProjectItems.AddFromDirectory(fullPath);
                    Marshal.ReleaseComObject(dir);
                }
            }
        }

        //public static void SetCode(string path, string newCode)
        //{
        //    ProjectItem projectItem = Project.ProjectItems.AddFromFile(path);

        //    EnvDTE.Window editWindow = projectItem.Open(Constants.vsext_vk_Code);
        //    editWindow.Visible = false;

        //    TextDocument textDocument = (TextDocument)editWindow.Document.Object("TextDocument");

        //    EditPoint editPoint = textDocument.StartPoint.CreateEditPoint();
        //    editPoint.Delete(textDocument.EndPoint);
        //    editPoint.Insert(newCode);

        //    editWindow.Close(vsSaveChanges.vsSaveChangesYes);

        //    Marshal.ReleaseComObject(textDocument);
        //    Marshal.ReleaseComObject(editPoint);
        //    Marshal.ReleaseComObject(projectItem);
        //}

        public static void ProcessFolder(MappingInfo mapping, ProjectFolderInfo folder)
        {
            if (folder == null || folder.ChildItems == null)
                return;

            if (folder.Checked == false)
                return;

            ProjectFolderRole role = folder.ProjectFolderRole;
            TridionRole tridionRole = TridionRole.Other;

            if (role == TridionVSRazorExtension.ProjectFolderRole.PageLayout)
            {
                tridionRole = TridionRole.PageLayoutContainer;
            }

            if (role == TridionVSRazorExtension.ProjectFolderRole.ComponentLayout)
            {
                tridionRole = TridionRole.ComponentLayoutContainer;
            }

            if (role == TridionVSRazorExtension.ProjectFolderRole.Binary)
            {
                tridionRole = TridionRole.MultimediaComponentContainer;
            }

            string tcmItemContainer = GetContainerTcmId(mapping, tridionRole, folder);

            if (String.IsNullOrEmpty(tcmItemContainer))
            {
                WriteErrorLog("Could not find Tridion mapping for role " + tridionRole);
                return;
            }

            foreach (ProjectItemInfo item in folder.ChildItems)
            {
                item.Parent = folder;

                if (item.IsFile)
                {
                    ProcessFile(mapping, (ProjectFileInfo)item);
                }
                if (item.IsFolder)
                {
                    ProcessFolder(mapping, (ProjectFolderInfo)item);
                }
            }
        }

        public static void ProcessFile(MappingInfo mapping, ProjectFileInfo file)
        {
            if (file == null)
                return;

            ShowMessage(file.Path + "...");

            if (!file.IsChecked() && !String.IsNullOrEmpty(file.TcmId))
                return;

            if (file.Checked != true)
                return;

            ProjectFolderRole role = ProjectFolderRole(file);
            bool syncTemplate = file.IsSyncTemplate();
            string templateFormat = TemplateFormat(file);
            List<string> schemaNames = file.SchemaNames;

            string titleItem = String.IsNullOrEmpty(file.Title) ? Path.GetFileNameWithoutExtension(file.FullPath) : file.Title;

            string tcmItemContainer = String.Empty;
            string tcmItem = String.Empty;

            string titleTemplate = String.Empty;
            string tcmTemplateContainer = String.Empty;

            TridionRole tridionRole = TridionRole.Other;

            if (role == TridionVSRazorExtension.ProjectFolderRole.PageLayout)
            {
                tridionRole = TridionRole.PageLayoutContainer;
                
                tcmItemContainer = GetContainerTcmId(mapping, TridionRole.PageLayoutContainer, file);
                tcmItem = String.IsNullOrEmpty(file.TcmId) ? GetItemTcmId(mapping, tcmItemContainer, titleItem) : file.TcmId;

                titleTemplate = String.IsNullOrEmpty(file.TemplateTitle) ? Path.GetFileNameWithoutExtension(file.FullPath) : file.TemplateTitle;
                tcmTemplateContainer = GetContainerTcmId(mapping, TridionRole.PageTemplateContainer, file.Path);
            }

            if (role == TridionVSRazorExtension.ProjectFolderRole.ComponentLayout)
            {
                titleItem = String.IsNullOrEmpty(file.Title) ? Path.GetFileNameWithoutExtension(file.FullPath) : file.Title;

                tridionRole = TridionRole.ComponentLayoutContainer;

                tcmItemContainer = GetContainerTcmId(mapping, TridionRole.ComponentLayoutContainer, file);
                tcmItem = String.IsNullOrEmpty(file.TcmId) ? GetItemTcmId(mapping, tcmItemContainer, titleItem) : file.TcmId;

                titleTemplate = String.IsNullOrEmpty(file.TemplateTitle) ? Path.GetFileNameWithoutExtension(file.FullPath) : file.TemplateTitle;
                tcmTemplateContainer = GetContainerTcmId(mapping, TridionRole.ComponentTemplateContainer, file.Path);
            }

            if (role == TridionVSRazorExtension.ProjectFolderRole.Binary)
            {
                tridionRole = TridionRole.MultimediaComponentContainer;

                tcmItemContainer = GetContainerTcmId(mapping, TridionRole.MultimediaComponentContainer, file);
                
                tcmItem = file.TcmId;
                if (String.IsNullOrEmpty(tcmItem))
                    tcmItem = GetItemTcmId(mapping, tcmItemContainer, Path.GetFileName(file.Path));
                if (String.IsNullOrEmpty(tcmItem))
                    tcmItem = GetItemTcmId(mapping, tcmItemContainer, Path.GetFileNameWithoutExtension(file.Path));
            }

            if (tridionRole == TridionRole.Other)
            {
                WriteErrorLog(String.Format("Role for item \"{0}\" is not detected", titleItem));
                ShowMessage(String.Empty);
                return;
            }

            if (String.IsNullOrEmpty(tcmItemContainer))
            {
                WriteErrorLog(String.Format("Could not find Tridion mapping for role {0}", tridionRole));
                ShowMessage(String.Empty);
                return;
            }

            //save (back) to file object
            file.TcmId = tcmItem;
            file.Title = titleItem;
            file.TemplateTitle = titleTemplate;

            //create sub-folder if file is not direct descedent
            if (file.Parent.FullPath.Trim('\\') != file.GetTopFolder().FullPath.Trim('\\'))
            {
                //get low-level folder
                tcmItemContainer = CreateFolderChain(mapping, file.Parent.FullPath.Trim('\\').Replace(file.GetTopFolder().FullPath.Trim('\\'), "").Trim('\\'), tcmItemContainer);
            }

            if (role == TridionVSRazorExtension.ProjectFolderRole.Binary)
            {
                SyncMultimedia(mapping, file, tcmItemContainer, mapping.TimeZoneId);
            }
            else
            {
                SyncRazorLayoutTbb(mapping, tcmItemContainer, file, role, mapping.TimeZoneId);
            }

            if (syncTemplate)
            {
                tcmItem = file.TcmId;

                //take template name from gialog
                if (!String.IsNullOrEmpty(file.TemplateTitle))
                    titleTemplate = file.TemplateTitle;

                if (String.IsNullOrEmpty(titleTemplate))
                    titleTemplate = titleItem;

                if(!String.IsNullOrEmpty(tcmItem) && !String.IsNullOrEmpty(tcmTemplateContainer) && !ExistsItem(mapping, tcmTemplateContainer, titleTemplate))
                {
                    if (role == TridionVSRazorExtension.ProjectFolderRole.PageLayout)
                    {
                        string stackTraceMessage;
                        bool res = SavePageTemplate(mapping, titleTemplate, string.Format(templateFormat, tcmItem, titleItem), tcmTemplateContainer, "cshtml", out stackTraceMessage);
                        if (!res)
                        {
                            WriteErrorLog(titleTemplate + " - Saving failed", stackTraceMessage);
                        }
                    }
                    if (role == TridionVSRazorExtension.ProjectFolderRole.ComponentLayout)
                    {
                        string stackTraceMessage;
                        bool res = SaveComponentTemplate(mapping, titleTemplate, string.Format(templateFormat, tcmItem, titleItem), tcmTemplateContainer, "HTML Fragment", false, out stackTraceMessage, schemaNames == null ? null : schemaNames.ToArray());
                        if (!res)
                        {
                            WriteErrorLog(titleTemplate + " - Saving failed", stackTraceMessage);
                        }
                    }
                }
            }

            ShowMessage(String.Empty);
        }

        private static string GetContainerTcmId(MappingInfo mapping, TridionRole tridionRole, ProjectFolderInfo folder, string path)
        {
            if (folder != null && !String.IsNullOrEmpty(folder.TcmId))
                return folder.TcmId;

            if (mapping.TridionFolders.Any(x => x.TridionRole == tridionRole))
            {
                if (mapping.TridionFolders.Count(x => x.TridionRole == tridionRole) == 1)
                {
                    string tcm = mapping.TridionFolders.First(x => x.TridionRole == tridionRole).TcmId;
                    if (folder != null)
                        folder.TcmId = tcm;
                    return tcm;
                }

                SelectTridionFolderDialogWindow dialog = new SelectTridionFolderDialogWindow();
                dialog.Path = path;
                dialog.TridionFolders = mapping.TridionFolders.Where(x => x.TridionRole == tridionRole).ToList().FillNamedPath(mapping);
                bool res = dialog.ShowDialog() == true;
                if (res)
                {
                    return dialog.SelectedTridionFolder.TcmId;
                }
            }

            return String.Empty;
        }

        private static string GetContainerTcmId(MappingInfo mapping, TridionRole tridionRole, string path)
        {
            return GetContainerTcmId(mapping, tridionRole, null, path);
        }

        private static string GetContainerTcmId(MappingInfo mapping, TridionRole tridionRole, ProjectFolderInfo folder)
        {
            if(folder == null)
                return String.Empty;
            return GetContainerTcmId(mapping, tridionRole, folder, folder.Path);
        }

        private static string GetContainerTcmId(MappingInfo mapping, TridionRole tridionRole, ProjectFileInfo file)
        {
            ProjectFolderInfo folder = GetTopFolder(file);
            return GetContainerTcmId(mapping, tridionRole, folder, file.Path);
        }

        public static List<TridionFolderInfo> FillNamedPath(this List<TridionFolderInfo> list, MappingInfo mapping)
        {
            foreach (TridionFolderInfo folder in list)
            {
                folder.FillNamedPath(mapping);
            }
            return list;
        }

        public static void FillNamedPath(this TridionFolderInfo tridionFolder, MappingInfo mapping)
        {
            if (tridionFolder == null || !String.IsNullOrEmpty(tridionFolder.NamedPath) || tridionFolder.TcmIdPath == null)
                return;

            List<string> names = new List<string>();
            foreach (string currTcmId in tridionFolder.TcmIdPath)
            {
                var item = ReadItem(mapping, currTcmId);
                if (item != null)
                    names.Add(item.Title);
            }
            names.Reverse();
            tridionFolder.NamedPath = string.Join("/", names);
        }

        public static bool IsChecked(this ProjectFileInfo file)
        {
            if (file == null)
                return false;

            return file.Checked == true;
        }

        public static bool IsChecked(this ProjectFolderInfo folder)
        {
            if (folder == null || folder.ChildItems == null)
                return false;

            return folder.ChildItems.Any(x => x.IsChecked());
        }

        public static bool IsChecked(this ProjectItemInfo item)
        {
            if (item.IsFolder)
                return ((ProjectFolderInfo)item).IsChecked();

            if (item.IsFile)
                return ((ProjectFileInfo)item).IsChecked();

            return false;
        }

        public static bool IsSyncTemplate(this ProjectFileInfo file)
        {
            if (file == null)
                return false;

            return file.SyncTemplate == true;
        }

        public static bool IsSyncTemplate(this ProjectFolderInfo folder)
        {
            if (folder == null || folder.ChildItems == null)
                return false;

            return folder.ChildItems.Any(x => x.IsSyncTemplate());
        }

        public static bool IsSyncTemplate(this ProjectItemInfo item)
        {
            if (item.IsFolder)
                return ((ProjectFolderInfo)item).IsSyncTemplate();

            if (item.IsFile)
                return ((ProjectFileInfo)item).IsSyncTemplate();

            return false;
        }

        public static string TemplateFormat(this ProjectFileInfo file)
        {
            ProjectFolderInfo folder = GetTopFolder(file);
            return folder != null ? folder.TemplateFormat : String.Empty;
        }

        public static ProjectFolderRole ProjectFolderRole(this ProjectFileInfo file)
        {
            ProjectFolderInfo folder = GetTopFolder(file);
            return folder != null ? folder.ProjectFolderRole : TridionVSRazorExtension.ProjectFolderRole.ComponentLayout;
        }

        public static ProjectFolderInfo GetTopFolder(this ProjectItemInfo item)
        {
            if (item == null)
                return null;

            ProjectFolderInfo parent = item.Parent;
            if ((parent == null || String.IsNullOrEmpty(parent.Path) || parent.ProjectFolderRole == TridionVSRazorExtension.ProjectFolderRole.Other) && item.IsFolder)
                return item as ProjectFolderInfo;

            return GetTopFolder(parent);
        }

        public static bool ExistsTcmId(this ProjectFolderInfo folder, string tcmId)
        {
            if (folder == null || folder.ChildItems == null)
                return false;

            foreach (ProjectItemInfo item in folder.ChildItems)
            {
                if (item.IsFile)
                {
                    if (tcmId == item.TcmId)
                        return true;
                }
                if (item.IsFolder)
                {
                    if (ExistsTcmId((ProjectFolderInfo)item, tcmId))
                        return true;
                }
            }
            return false;
        }

        public static bool Equals(string content1, string content2)
        {
            return content1.RemoveSpaces() == content2.RemoveSpaces() ||
                TransformMediator2Razor(content1).RemoveSpaces() == content2.RemoveSpaces() ||
                content1.RemoveSpaces() == TransformMediator2Razor(content2).RemoveSpaces() ||
                TransformRazor2Mediator(content1).RemoveSpaces() == content2.RemoveSpaces() ||
                content1.RemoveSpaces() == TransformRazor2Mediator(content2).RemoveSpaces();
        }

        public static void ProcessTridionFolder(MappingInfo mapping, TridionFolderInfo folder)
        {
            if (!folder.ScanForItems || string.IsNullOrEmpty(folder.TcmId) || (folder.TridionRole != TridionRole.PageLayoutContainer && folder.TridionRole != TridionRole.ComponentLayoutContainer))
                return;

            List<ItemInfo> items = GetTbbsByParentFolder(mapping, folder.TcmId);

            foreach (ItemInfo item in items)
            {
                ProcessTridionItem(mapping, item, folder);
            }

            List<ItemInfo> childFolderItems = GetFoldersByParentFolder(mapping, folder.TcmId);

            if (childFolderItems != null && childFolderItems.Count > 0)
            {
                List<TridionFolderInfo> childFolders = childFolderItems.Select(childFolderItem => new TridionFolderInfo { TcmId = childFolderItem.TcmId, TridionRole = folder.TridionRole, ScanForItems = folder.ScanForItems, ParentFolder = folder }).ToList();

                folder.ChildFolders = childFolders;

                foreach (TridionFolderInfo childFolder in childFolders)
                {
                    ProcessTridionFolder(mapping, childFolder);
                }
            }
        }

        public static void ProcessTridionItem(MappingInfo mapping, ItemInfo item, TridionFolderInfo folder)
        {
            //todo: nested path

            if (item == null || folder == null)
                return;

            if (!folder.ScanForItems)
                return;

            if (mapping.ProjectFolders.Any(projectFolder => projectFolder.ExistsTcmId(item.TcmId)))
                return;

            if (folder.TridionRole == TridionRole.PageLayoutContainer || folder.TridionRole == TridionRole.ComponentLayoutContainer)
            {
                TemplateBuildingBlockData tridionItem = ReadItem(mapping, item.TcmId) as TemplateBuildingBlockData;
                if (tridionItem == null || tridionItem.VersionInfo.RevisionDate == null)
                    return;

                ShowMessage(item.Title + "...");

                ProjectDestinationDialogWindow dialog = new ProjectDestinationDialogWindow();
                dialog.Mapping = mapping;
                dialog.TridionRole = folder.TridionRole;
                dialog.TridionTcmId = item.TcmId;
                dialog.TridionTitle = item.Title;
                dialog.TridionContent = tridionItem.Content;

                bool res = dialog.ShowDialog() == true;
                if (res)
                {
                    ProjectFolderInfo projectFolder = dialog.ProjectFolder;
                    ProjectFileInfo projectFile = dialog.ProjectFile;

                    if (projectFolder != null && projectFile != null)
                    {
                        string path = projectFile.FullPath;
                        DateTime tridionDate = (DateTime) tridionItem.VersionInfo.RevisionDate;
                        DateTime tridionLocalDate = tridionDate.GetLocalTime(mapping.TimeZoneId);
                        SaveVSItem(path, tridionItem.Content);
                        File.SetAttributes(path, FileAttributes.Normal);
                        File.SetLastWriteTime(path, tridionLocalDate);
                        WriteSuccessLog(path + " - Saved to Visual Studio");
                    }
                }

                ShowMessage(String.Empty);
            }

            if (folder.TridionRole == TridionRole.MultimediaComponentContainer)
            {
                ComponentData tridionItem = GetComponent(mapping, item.TcmId);
                if (tridionItem == null || tridionItem.VersionInfo.RevisionDate == null)
                    return;

                ShowMessage(item.Title + "...");

                ProjectBinaryDestinationDialogWindow dialog = new ProjectBinaryDestinationDialogWindow();
                dialog.Mapping = mapping;
                dialog.TridionTcmId = item.TcmId;
                dialog.TridionTitle = item.Title;

                bool res = dialog.ShowDialog() == true;
                if (res)
                {
                    ProjectFolderInfo projectFolder = dialog.ProjectFolder;
                    ProjectFileInfo projectFile = dialog.ProjectFile;

                    if (projectFolder != null && projectFile != null)
                    {
                        string path = projectFile.FullPath;
                        DateTime tridionDate = (DateTime)tridionItem.VersionInfo.RevisionDate;
                        DateTime tridionLocalDate = tridionDate.GetLocalTime(mapping.TimeZoneId);
                        SaveVSBinaryItem(mapping, projectFile.TcmId, path);
                        File.SetAttributes(path, FileAttributes.Normal);
                        File.SetLastWriteTime(path, tridionLocalDate);
                        WriteSuccessLog(path + " - Saved to Visual Studio successfully");
                    }
                }

                ShowMessage(String.Empty);
            }
        }

        public static void SyncMultimedia(MappingInfo mapping, ProjectFileInfo file, string tcmContainer, string serverTimeZoneId)
        {
            string path = file.FullPath;

            if (!File.Exists(path))
            {
                //remove non-existing file from mapping
                foreach (ProjectFolderInfo folder in mapping.ProjectFolders)
                {
                    DeleteFileFromMapping(folder, path);
                }
                return;
            }

            IncludeProjectItem(path);

            string title;
            if (String.IsNullOrEmpty(file.Title))
            {
                title = Path.GetFileNameWithoutExtension(path);
                file.Title = title;
            }
            else
            {
                title = file.Title;
            }

            string id;
            if (String.IsNullOrEmpty(file.TcmId))
            {
                id = GetItemTcmId(mapping, tcmContainer, title);
                if (String.IsNullOrEmpty(id))
                    id = GetItemTcmId(mapping, tcmContainer, Path.GetFileNameWithoutExtension(path));
                if (String.IsNullOrEmpty(id))
                    id = GetItemTcmId(mapping, tcmContainer, Path.GetFileName(path));

                file.TcmId = id;
            }
            else
            {
                id = file.TcmId;
            }

            if (!File.Exists(path) && String.IsNullOrEmpty(id))
                return;

            bool updated = false;

            if (File.Exists(path) && String.IsNullOrEmpty(id))
            {
                TridionDestinationDialogWindow dialog = new TridionDestinationDialogWindow();
                dialog.Mapping = mapping;
                dialog.FilterItemTcmId = tcmContainer;
                dialog.PublicationTcmId = GetPublicationTcmId(tcmContainer);
                dialog.LayoutTitle = title;
                bool res = dialog.ShowDialog() == true;
                if (res)
                {
                    file.Title = dialog.LayoutTitle;
                    tcmContainer = GetBluePrintItemTcmId(tcmContainer, dialog.PublicationTcmId);
                    string stackTraceMessage;
                    updated = SaveMultimediaComponentFromBinary(mapping, path, file.Title, tcmContainer, out stackTraceMessage);
                    if (updated)
                    {
                        id = GetItemTcmId(mapping, tcmContainer, Path.GetFileName(path));
                        if (String.IsNullOrEmpty(id))
                            id = GetItemTcmId(mapping, tcmContainer, Path.GetFileNameWithoutExtension(path));

                        if (String.IsNullOrEmpty(id))
                        {
                            WriteErrorLog(file.Path + " - Item is broken after updating");
                            return;
                        }
                        file.TcmId = id;
                    }
                    else
                    {
                        WriteErrorLog(path + " - Creating failed", stackTraceMessage);
                        return;
                    }
                }
            }

            ComponentData item = ReadItem(mapping, id) as ComponentData;

            if (item == null || item.VersionInfo.RevisionDate == null)
            {
                WriteErrorLog(string.Format("Item {0} does not exist", id));
                return;
            }

            FileInfo fi = new FileInfo(path);
            DateTime fileDate = fi.LastWriteTime;
            DateTime tridionDate = (DateTime)item.VersionInfo.RevisionDate;
            DateTime tridionLocalDate = tridionDate.GetLocalTime(serverTimeZoneId);

            if (updated)
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.SetLastWriteTime(path, tridionLocalDate);
                WriteSuccessLog(path + " - Saved to Tridion CM successfully");
                return;
            }

            if (fileDate == tridionLocalDate)
            {
                return;
            }

            file.Title = item.Title;

            if (fi.Length == item.BinaryContent.FileSize)
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.SetLastWriteTime(path, tridionLocalDate);
                WriteSuccessLog(path + " - Saved to Tridion CM successfully");
            }
            else
            {
                BinaryDiffDialogWindow dialog = new BinaryDiffDialogWindow();
                if (fileDate > tridionLocalDate)
                {
                    dialog.StartItemInfo = string.Format("Tridion Item: {0} ({1}), {2}", item.Title, item.Id, tridionLocalDate);
                    dialog.EndItemInfo = string.Format("VS File: {0}, {1}", Path.GetFileName(path), fileDate);
                    dialog.SyncState = SyncState.VS2Tridion;
                    dialog.Tridion2VSEnabled = true;
                    dialog.VS2TridionEnabled = !IsCheckedOut(mapping, item.Id);
                }
                else
                {
                    dialog.StartItemInfo = string.Format("VS File: {0}, {1}", Path.GetFileName(path), fileDate);
                    dialog.EndItemInfo = string.Format("Tridion Item: {0} ({1}), {2}", item.Title, item.Id, tridionLocalDate);
                    dialog.SyncState = SyncState.Tridion2VS;
                    dialog.Tridion2VSEnabled = true;
                    dialog.VS2TridionEnabled = true;
                }

                bool res = dialog.ShowDialog() == true;
                if (res)
                {
                    if (dialog.SyncState == SyncState.VS2Tridion)
                    {
                        string stackTraceMessage;
                        updated = SaveMultimediaComponentFromBinary(mapping, id, path, out stackTraceMessage);
                        if (updated)
                        {
                            item = ReadItem(mapping, id) as ComponentData;
                            if (item == null || item.VersionInfo.RevisionDate == null)
                            {
                                WriteErrorLog(string.Format("Item {0} does not exist", id));
                            }
                            else
                            {
                                tridionDate = (DateTime)item.VersionInfo.RevisionDate;
                                tridionLocalDate = tridionDate.GetLocalTime(serverTimeZoneId);
                                File.SetAttributes(path, FileAttributes.Normal);
                                File.SetLastWriteTime(path, tridionLocalDate);
                                WriteSuccessLog(path + " - Saved to Tridion CM successfully");
                            }
                        }
                        else
                        {
                            WriteErrorLog(path + " - Updating failed", stackTraceMessage);
                        }
                    }
                    else if (dialog.SyncState == SyncState.Tridion2VS)
                    {
                        SaveVSBinaryItem(mapping, id, path);
                        File.SetAttributes(path, FileAttributes.Normal);
                        File.SetLastWriteTime(path, tridionLocalDate);
                        WriteSuccessLog(path + " - Saved to Visual Studio successfully");
                    }
                }
            }
        }

        private static void SaveVSBinaryItem(MappingInfo mapping, string id, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!String.IsNullOrEmpty(directory))
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    ProjectItem dir = Project.ProjectItems.AddFromDirectory(directory);
                    Marshal.ReleaseComObject(dir);
                }
            }

            SaveBinaryFromMultimediaComponent(mapping, id, directory);

            IncludeProjectItem(path);
        }

        public static void ShowMessage(string message)
        {
            if (TxtLog == null)
                return;

            TxtLog.Text = message;
            System.Windows.Forms.Application.DoEvents();
        }

        public static void WriteSuccessLog(string message)
        {
            File.AppendAllText(Path.Combine(RootPath, "TridionRazorExtensionLog.txt"), string.Format("SUCCESS - {0} - {1}\r\n", DateTime.Now, message));
        }

        public static void WriteErrorLog(string message, string stackTraceMessage = "")
        {
            string resMessage = String.Format("ERROR   - {0} - {1}\r\n", DateTime.Now, message);
            if (!String.IsNullOrEmpty(stackTraceMessage))
            {
                resMessage += "===============================================\r\n" + stackTraceMessage + "\r\n===============================================\r\n";
            }
            MessageBox.Show(resMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            File.AppendAllText(Path.Combine(RootPath, "TridionRazorExtensionLog.txt"), resMessage);
        }

        public static void ProcessFiles(string[] filePaths, Project project)
        {
            if (filePaths == null || filePaths.Length == 0)
                return;

            string rootPath = Path.GetDirectoryName(project.FileName);

            RootPath = rootPath;
            Project = project;

            Configuration configuration = GetConfiguration(rootPath, "TridionRazorMapping.xml");

            MappingInfo mapping = configuration.FirstOrDefault(x => x.Name == (configuration.DefaultConfiguration ?? "Default"));
            if (mapping == null)
            {
                MessageBox.Show("Configuration doesn't contain default mapping. Please edit configuration.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (string filePath in filePaths)
            {
                List<ProjectFileInfo> files = mapping.GetFiles(filePath);
                List<ProjectFolderInfo> folders = mapping.GetFolders(filePath);

                if (files == null || files.Count == 0 || folders.Count == 0)
                {
                    MessageBox.Show("Item '" + filePath + "' is not mapped. Please edit mapping.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (files.Count > 1 || folders.Count > 1)
                {
                    MessageBox.Show("Item '" + filePath + "' is mapped to more than one item. Please edit mapping.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            foreach (string filePath in filePaths)
            {
                List<ProjectFileInfo> files = mapping.GetFiles(filePath);
                List<ProjectFolderInfo> folders = mapping.GetFolders(filePath);

                if (files.Count == 1 && folders.Count == 1)
                {
                    ProjectFileInfo file = files[0];
                    file.Parent = folders[0];
                    ProcessFile(mapping, file);
                }
            }

            CheckRazorWebConfig(mapping);

            SaveConfiguration(rootPath, "TridionRazorMapping.xml", configuration);

            MessageBox.Show("Synchronization finished", "Finish", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void ProcessFolders(string[] folderPaths, Project project)
        {
            if (folderPaths == null || folderPaths.Length == 0)
                return;

            string rootPath = Path.GetDirectoryName(project.FileName);

            RootPath = rootPath;
            Project = project;

            Configuration configuration = GetConfiguration(rootPath, "TridionRazorMapping.xml");

            MappingInfo mapping = configuration.FirstOrDefault(x => x.Name == (configuration.DefaultConfiguration ?? "Default"));
            if (mapping == null)
            {
                MessageBox.Show("Configuration doesn't contain default mapping. Please edit configuration.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (string folderPath in folderPaths)
            {
                List<ProjectFolderInfo> folders = mapping.GetFoldersByPath(folderPath);

                if (folders == null || folders.Count == 0)
                {
                    MessageBox.Show("Folder '" + folderPath + "' is not mapped. Please edit mapping.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (folders.Count > 1)
                {
                    MessageBox.Show("Folder '" + folderPath + "' is mapped to more than one item. Please edit mapping.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            foreach (string folderPath in folderPaths)
            {
                List<ProjectFolderInfo> folders = mapping.GetFoldersByPath(folderPath);

                if (folders.Count == 1)
                {
                    ProcessFolder(mapping, folders[0]);
                }
            }

            CheckRazorWebConfig(mapping);

            SaveConfiguration(rootPath, "TridionRazorMapping.xml", configuration);

            MessageBox.Show("Synchronization finished", "Finish", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static List<ProjectFileInfo> GetFiles(this MappingInfo mapping, string filePath)
        {
            List<ProjectFileInfo> res = new List<ProjectFileInfo>();

            if (mapping == null)
                return res;

            foreach (ProjectFolderInfo projectFolder in mapping.ProjectFolders)
            {
                if (projectFolder.ChildItems == null)
                    continue;

                foreach (ProjectFileInfo file in projectFolder.ChildItems.Where(x => x.IsFile))
                {
                    if (file.FullPath == filePath && file.IsChecked())
                    {
                        file.Parent = projectFolder;
                        res.Add(file);
                    }
                }

                foreach (ProjectFolderInfo folder in projectFolder.ChildItems.Where(x => x.IsFolder))
                {
                    res.AddRange(folder.GetFiles(filePath));
                }
            }

            return res;
        }

        public static List<ProjectFileInfo> GetFiles(this ProjectFolderInfo folder, string filePath)
        {
            List<ProjectFileInfo> res = new List<ProjectFileInfo>();

            if (folder.ChildItems == null)
                return res;

            foreach (ProjectFileInfo file in folder.ChildItems.Where(x => x.IsFile))
            {
                if (file.FullPath == filePath && file.IsChecked())
                {
                    file.Parent = folder;
                    res.Add(file);
                }
            }

            foreach (ProjectFolderInfo childFolder in folder.ChildItems.Where(x => x.IsFolder))
            {
                res.AddRange(childFolder.GetFiles(filePath));
            }

            return res;
        }

        public static List<ProjectFolderInfo> GetFolders(this MappingInfo mapping, string filePath)
        {
            List<ProjectFolderInfo> res = new List<ProjectFolderInfo>();

            if (mapping == null)
                return res;
            
            foreach (ProjectFolderInfo projectFolder in mapping.ProjectFolders)
            {
                if (projectFolder.ChildItems == null)
                    continue;

                foreach (ProjectFileInfo file in projectFolder.ChildItems.Where(x => x.IsFile))
                {
                    if (file.FullPath == filePath && file.IsChecked())
                    {
                        res.Add(projectFolder);
                    }
                }

                foreach (ProjectFolderInfo folder in projectFolder.ChildItems.Where(x => x.IsFolder))
                {
                    res.AddRange(folder.GetFolders(filePath));
                }
            }

            return res;
        }

        public static List<ProjectFolderInfo> GetFolders(this ProjectFolderInfo folder, string filePath)
        {
            List<ProjectFolderInfo> res = new List<ProjectFolderInfo>();

            if (folder.ChildItems == null)
                return res;

            foreach (ProjectFileInfo file in folder.ChildItems.Where(x => x.IsFile))
            {
                if (file.FullPath == filePath && file.IsChecked())
                {
                    res.Add(folder);
                }
            }

            foreach (ProjectFolderInfo childFolder in folder.ChildItems.Where(x => x.IsFolder))
            {
                res.AddRange(childFolder.GetFolders(filePath));
            }

            return res;
        }

        public static List<ProjectFolderInfo> GetFoldersByPath(this MappingInfo mapping, string path)
        {
            List<ProjectFolderInfo> res = new List<ProjectFolderInfo>();

            foreach (ProjectFolderInfo projectFolder in mapping.ProjectFolders)
            {
                if (projectFolder.FullPath.Trim('\\') == path.Trim('\\') && projectFolder.Checked != false)
                {
                    res.Add(projectFolder);
                }

                res.AddRange(projectFolder.GetFoldersByPath(path));
            }
            return res;
        }

        public static List<ProjectFolderInfo> GetFoldersByPath(this ProjectFolderInfo folder, string path)
        {
            List<ProjectFolderInfo> res = new List<ProjectFolderInfo>();

            if (folder.ChildItems == null)
                return res;

            foreach (ProjectFolderInfo childFolder in folder.ChildItems.Where(x => x.IsFolder))
            {
                if (childFolder.FullPath.Trim('\\') == path.Trim('\\') && childFolder.Checked != false)
                {
                    res.Add(folder);
                }

                res.AddRange(childFolder.GetFoldersByPath(path));
            }

            return res;
        }

        public static void CheckRazorWebConfig(MappingInfo mapping)
        {
            List<string> paths = new List<string>();
            foreach (ProjectFolderInfo projectFolder in mapping.ProjectFolders)
            {
                if (projectFolder.ProjectFolderRole == TridionVSRazorExtension.ProjectFolderRole.PageLayout || projectFolder.ProjectFolderRole == TridionVSRazorExtension.ProjectFolderRole.ComponentLayout)
                    paths.Add(projectFolder.FullPath);
            }

            if(paths.Count == 0)
                return;

            string path = GetRootPath(paths, paths[0]);
            path = Path.Combine(path, "Web.config");

            //create web config
            if (!File.Exists(path))
            {
                string content = Resources.Web;

                List<string> assemblies = new List<string> { "Tridion.Extensions.Mediators.Razor", "Tridion.Common", "Tridion.ContentManager", "Tridion.ContentManager.Templating", "Tridion.ContentManager.Common" };

                foreach (string name in assemblies)
                {
                    if (content.Contains(string.Format("assembly=\"{0}\"", name)))
                    {
                        string fullName = GetAssembly(name);
                        if (!String.IsNullOrEmpty(fullName))
                            content = content.Replace(string.Format("assembly=\"{0}\"", name), string.Format("assembly=\"{0}\"", fullName));
                    }
                }

                File.WriteAllText(path, content);

                IncludeProjectItem(path);
            }
        }

        private static string GetRootPath(List<string> paths, string currPath)
        {
            if (paths == null)
                return currPath;

            if (paths.All(path => path.StartsWith(currPath)))
                return currPath;

            return GetRootPath(paths, Path.GetDirectoryName(currPath));
        }

        #endregion

        #region Delete helpers

        public static void DeleteFiles(string[] filePaths, Project project)
        {
            if (filePaths == null || filePaths.Length == 0)
                return;

            string rootPath = Path.GetDirectoryName(project.FileName);

            RootPath = rootPath;
            Project = project;

            Configuration configuration = GetConfiguration(rootPath, "TridionRazorMapping.xml");

            MappingInfo mapping = configuration.FirstOrDefault(x => x.Name == (configuration.DefaultConfiguration ?? "Default"));

            bool res = true;
            foreach (string filePath in filePaths)
            {
                if (!res)
                    continue;

                if (MessageBox.Show(String.Format("Are you sure you want to delete item \"{0}\"", Path.GetFileName(filePath)), "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    List<ProjectFileInfo> files = mapping.GetFiles(filePath);
                    foreach (ProjectFileInfo file in files)
                    {
                        if(!res)
                            continue;

                        if (String.IsNullOrEmpty(file.TcmId))
                        {
                            res = false;
                            continue;
                        }
                        
                        //delete from Tridion
                        string stackTraceMessage;
                        res = DeleteTridionObject(mapping, file.TcmId, string.Empty, true, out stackTraceMessage);
                        if (!res)
                        {
                            WriteErrorLog(String.Format("Error deleting item {0}({1})", Path.GetFileName(file.Path), file.TcmId), stackTraceMessage);
                        }
                    }

                    if (res)
                    {
                        //delete from project
                        ProjectItem item = Project.ProjectItems.AddFromFile(filePath);
                        item.Delete();
                        Marshal.ReleaseComObject(item);

                        //remove from mapping
                        if (mapping != null)
                        {
                            foreach (ProjectFolderInfo folder in mapping.ProjectFolders)
                            {
                                DeleteFileFromMapping(folder, filePath);
                            }
                        }
                    }

                    //delete from file system
                    if (res && File.Exists(filePath))
                        File.Delete(filePath);
                }
            }

            SaveConfiguration(rootPath, "TridionRazorMapping.xml", configuration);

            if (res)
                MessageBox.Show("Item(s) deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void DeleteFileFromMapping(ProjectFolderInfo folder, string filePath)
        {
            if (folder.ChildItems == null)
                return;

            folder.ChildItems.RemoveAll(x => x.IsFile && x.FullPath == filePath);

            foreach (ProjectFolderInfo childFolder in folder.ChildItems.Where(x => x.IsFolder))
            {
                DeleteFileFromMapping(childFolder, filePath);
            }
        }

        public static XElement GetComponentLink(string id, string title, string fieldName)
        {
            XNamespace ns = "http://www.w3.org/1999/xlink";

            if (string.IsNullOrEmpty(title))
                return new XElement(fieldName,
                    new XAttribute(XNamespace.Xmlns + "xlink", ns),
                    new XAttribute(ns + "href", id));

            return new XElement(fieldName,
                new XAttribute(XNamespace.Xmlns + "xlink", ns),
                new XAttribute(ns + "href", id),
                new XAttribute(ns + "title", title));
        }

        private static List<TbbInfo> GetTbbList(string templateContent)
        {
            List<TbbInfo> tbbList = new List<TbbInfo>();

            XNamespace ns = "http://www.tridion.com/ContentManager/5.3/CompoundTemplate";
            XNamespace linkNs = "http://www.w3.org/1999/xlink";

            XDocument xml = XDocument.Parse(templateContent);

            if (xml.Root == null)
                return tbbList;

            List<XElement> templateInvocations = xml.Root.Elements(ns + "TemplateInvocation").ToList();
            foreach (XElement invovation in templateInvocations)
            {
                TbbInfo tbbInfo = new TbbInfo();

                XElement template = invovation.Elements(ns + "Template").FirstOrDefault();
                if (template != null)
                {
                    tbbInfo.TcmId = template.Attribute(linkNs + "href").Value;
                    tbbInfo.Title = template.Attribute(linkNs + "title").Value;
                }

                XElement templateParameters = invovation.Elements(ns + "TemplateParameters").FirstOrDefault();
                if (templateParameters != null)
                {
                    tbbInfo.TemplateParameters = templateParameters;
                }

                tbbList.Add(tbbInfo);
            }

            return tbbList;
        }

        private static string GetTemplateContent(List<TbbInfo> tbbList)
        {
            XNamespace ns = "http://www.tridion.com/ContentManager/5.3/CompoundTemplate";

            XElement root = new XElement(ns + "CompoundTemplate");
            foreach (TbbInfo tbbInfo in tbbList)
            {
                XElement templateInvocation = new XElement(ns + "TemplateInvocation");

                XElement template = GetComponentLink(tbbInfo.TcmId, tbbInfo.Title, "Template");
                if (template != null)
                    templateInvocation.Add(template);

                if (tbbInfo.TemplateParameters != null)
                    templateInvocation.Add(tbbInfo.TemplateParameters);

                root.Add(templateInvocation);
            }

            return root.ToString().Replace(" xmlns=\"\"", "");
        }

        private static string RemoveTbbFromTemplate(string templateContent, string tcmTbb)
        {
            List<TbbInfo> tbbList = GetTbbList(templateContent).Where(x => x.TcmId.Split('-')[1] != tcmTbb.Split('-')[1]).ToList();
            return GetTemplateContent(tbbList);
        }

        private static LinkStatus RemoveTbbFromPageTemplate(MappingInfo mapping, string tcmPageTemplate, string tcmTbb, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            PageTemplateData pageTemplate = ReadItem(mapping, tcmPageTemplate) as PageTemplateData;
            if (pageTemplate == null)
                return LinkStatus.NotFound;

            List<TbbInfo> tbbList = GetTbbList(pageTemplate.Content);
            if (tbbList.Any(x => x.TcmId.Split('-')[1] == tcmTbb.Split('-')[1]))
            {
                if (tbbList.Count == 1)
                    return LinkStatus.Mandatory;
            }
            else
            {
                return LinkStatus.NotFound;
            }

            string newContent = RemoveTbbFromTemplate(pageTemplate.Content, tcmTbb);

            if (pageTemplate.BluePrintInfo.IsShared == true)
            {
                tcmPageTemplate = GetBluePrintTopTcmId(tcmPageTemplate);

                pageTemplate = ReadItem(mapping, tcmPageTemplate) as PageTemplateData;
                if (pageTemplate == null)
                    return LinkStatus.NotFound;
            }

            try
            {
                pageTemplate = Client.CheckOut(pageTemplate.Id, true, new ReadOptions()) as PageTemplateData;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                return LinkStatus.NotFound;
            }

            if (pageTemplate == null)
                return LinkStatus.NotFound;

            pageTemplate.Content = newContent;

            try
            {
                pageTemplate = Client.Update(pageTemplate, new ReadOptions()) as PageTemplateData;
                if (pageTemplate == null)
                    return LinkStatus.NotFound;

                Client.CheckIn(pageTemplate.Id, new ReadOptions());
                return LinkStatus.Found;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;

                if (pageTemplate == null)
                    return LinkStatus.Error;

                Client.UndoCheckOut(pageTemplate.Id, true, new ReadOptions());
                return LinkStatus.Error;
            }
        }

        private static LinkStatus RemoveTbbFromComponentTemplate(MappingInfo mapping, string tcmComponentTemplate, string tcmTbb, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            ComponentTemplateData componentTemplate = ReadItem(mapping, tcmComponentTemplate) as ComponentTemplateData;
            if (componentTemplate == null)
                return LinkStatus.NotFound;

            List<TbbInfo> tbbList = GetTbbList(componentTemplate.Content);
            if (tbbList.Any(x => x.TcmId.Split('-')[1] == tcmTbb.Split('-')[1]))
            {
                if (tbbList.Count == 1)
                    return LinkStatus.Mandatory;
            }
            else
            {
                return LinkStatus.NotFound;
            }

            string newContent = RemoveTbbFromTemplate(componentTemplate.Content, tcmTbb);

            if (componentTemplate.BluePrintInfo.IsShared == true)
            {
                tcmComponentTemplate = GetBluePrintTopTcmId(tcmComponentTemplate);

                componentTemplate = ReadItem(mapping, tcmComponentTemplate) as ComponentTemplateData;
                if (componentTemplate == null)
                    return LinkStatus.NotFound;
            }

            try
            {
                componentTemplate = Client.CheckOut(componentTemplate.Id, true, new ReadOptions()) as ComponentTemplateData;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                return LinkStatus.NotFound;
            }

            if (componentTemplate == null)
                return LinkStatus.NotFound;

            componentTemplate.Content = newContent;

            try
            {
                componentTemplate = Client.Update(componentTemplate, new ReadOptions()) as ComponentTemplateData;
                if (componentTemplate == null)
                    return LinkStatus.NotFound;

                Client.CheckIn(componentTemplate.Id, new ReadOptions());
                return LinkStatus.Found;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;

                if (componentTemplate == null)
                    return LinkStatus.Error;

                Client.UndoCheckOut(componentTemplate.Id, true, new ReadOptions());
                return LinkStatus.Error;
            }
        }

        private static LinkStatus RemoveHistory(string tcmItem, string parentTcmId, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            List<HistoryItemInfo> history = GetItemHistory(tcmItem);

            if (history.Count <= 1)
                return LinkStatus.Mandatory;

            LinkStatus status = LinkStatus.NotFound;
            foreach (HistoryItemInfo historyItem in history)
            {
                if (historyItem.TcmId == history.Last().TcmId)
                    continue;

                List<string> historyItemUsedItems = GetUsedItems(historyItem.TcmId);
                if (historyItemUsedItems.Any(x => x.Split('-')[1] == parentTcmId.Split('-')[1]))
                {
                    try
                    {
                        Client.Delete(historyItem.TcmId);
                        status = LinkStatus.Found;
                    }
                    catch (Exception ex)
                    {
                        stackTraceMessage = ex.Message;
                        return LinkStatus.Error;
                    }
                }
            }

            return status;
        }

        private static LinkStatus RemoveDependency(MappingInfo mapping, string tcmItem, string tcmDependentItem)
        {
            ItemType itemType = GetItemType(tcmItem);
            ItemType dependentItemType = GetItemType(tcmDependentItem);
            LinkStatus status = LinkStatus.NotFound;
            string stackTraceMessage = "";

            //remove TBB from page template
            if (itemType == ItemType.PageTemplate && dependentItemType == ItemType.TemplateBuildingBlock)
            {
                status = RemoveTbbFromPageTemplate(mapping, tcmItem, tcmDependentItem, out stackTraceMessage);
            }

            //remove TBB from component template
            if (itemType == ItemType.ComponentTemplate && dependentItemType == ItemType.TemplateBuildingBlock)
            {
                status = RemoveTbbFromComponentTemplate(mapping, tcmItem, tcmDependentItem, out stackTraceMessage);
            }

            if (status == LinkStatus.Found)
            {
                status = RemoveHistory(tcmItem, tcmDependentItem, out stackTraceMessage);
            }

            if (status == LinkStatus.Error)
            {
                WriteErrorLog(string.Format("Not able to unlink \"{1}\" from \"{0}\"", tcmItem, tcmDependentItem), stackTraceMessage);
            }

            return status;
        }

        private static bool DeleteTridionObject(MappingInfo mapping, string tcmItem, string parentTcmId, bool currentVersion, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            if (tcmItem.StartsWith("tcm:0-"))
                return false;

            if (!EnsureValidClient(mapping))
                return false;

            tcmItem = GetBluePrintTopTcmId(mapping, tcmItem);

            bool isAnyLocalized = IsAnyLocalized(tcmItem);

            List<string> usingItems = GetUsingItems(tcmItem);
            List<string> usingCurrentItems = GetUsingCurrentItems(tcmItem);

            if (currentVersion)
            {
                foreach (string usingItem in usingItems)
                {
                    LinkStatus status = RemoveDependency(mapping, usingItem, tcmItem);
                    if (status == LinkStatus.Error)
                    {
                        return false;
                    }
                    if (status != LinkStatus.Found)
                    {
                        DeleteTridionObject(mapping, usingItem, tcmItem, usingCurrentItems.Any(x => x == usingItem), out stackTraceMessage);
                    }
                }
            }

            try
            {
                if (!currentVersion)
                {
                    //remove used versions
                    LinkStatus status = RemoveHistory(tcmItem, parentTcmId, out stackTraceMessage);
                    if (status == LinkStatus.Error)
                        return false;
                }
                else
                {
                    //unlocalize before delete
                    if (isAnyLocalized)
                    {
                        UnLocalizeAll(tcmItem);
                    }

                    //undo checkout
                    try
                    {
                        Client.UndoCheckOut(tcmItem, true, new ReadOptions());
                    }
                    catch (Exception)
                    {
                    }

                    //delete used item
                    Client.Delete(tcmItem);
                }
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                return false;
            }

            return true;
        }
        
        #endregion

        #region Tridion Blueprint

        public static string GetBluePrintTopTcmId(MappingInfo mapping, string id)
        {
            if (!EnsureValidClient(mapping))
                return String.Empty;

            return GetBluePrintTopTcmId(id);
        }

        private static string GetBluePrintTopTcmId(string id)
        {
            if (id.StartsWith("tcm:0-"))
                return id;

            var list = Client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = id } });
            if (list == null || list.Length == 0)
                return id;

            var list2 = list.Cast<BluePrintNodeData>().Where(x => x.Item != null).ToList();

            return list2.First().Item.Id;
        }

        public static string GetBluePrintBottomTcmId(MappingInfo mapping, string id)
        {
            if (!EnsureValidClient(mapping))
                return String.Empty;

            return GetBluePrintBottomTcmId(id);
        }

        private static string GetBluePrintBottomTcmId(string id)
        {
            if (id.StartsWith("tcm:0-"))
                return id;

            var list = Client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = id } });
            if (list == null || list.Length == 0)
                return id;

            var list2 = list.Cast<BluePrintNodeData>().Where(x => x.Item != null).ToList();

            return list2.Last().Item.Id;
        }

        public static string GetBluePrintTopLocalizedTcmId(MappingInfo mapping, string id)
        {
            if (!EnsureValidClient(mapping))
                return String.Empty;

            return GetBluePrintTopLocalizedTcmId(id);
        }

        private static string GetBluePrintTopLocalizedTcmId(string id)
        {
            if (id.StartsWith("tcm:0-"))
                return id;

            var list = Client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = id } });
            if (list == null || list.Length == 0)
                return id;

            var item = list.Cast<BluePrintNodeData>().FirstOrDefault(x => x.Item != null && x.Item.Id == id);
            if (item == null)
                return id;

            string publicationId = item.Item.BluePrintInfo.OwningRepository.IdRef;

            return GetBluePrintItemTcmId(id, publicationId);
        }

        public static bool IsLocalized(MappingInfo mapping, string id)
        {
            if (!EnsureValidClient(mapping))
                return false;

            return IsLocalized(id);
        }

        private static bool IsLocalized(string id)
        {
            return id == GetBluePrintTopLocalizedTcmId(id) && id != GetBluePrintTopTcmId(id);
        }

        public static bool IsAnyLocalized(MappingInfo mapping, string id)
        {
            if (!EnsureValidClient(mapping))
                return false;

            return IsAnyLocalized(id);
        }

        private static bool IsAnyLocalized(string id)
        {
            var list = Client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = id } });
            if (list == null || list.Length == 0)
                return false;

            return list.Cast<BluePrintNodeData>().Any(x => x.Item != null && IsLocalized(x.Item.Id));
        }

        public static bool IsShared(MappingInfo mapping, string id)
        {
            if (!EnsureValidClient(mapping))
                return false;

            return IsShared(id);
        }

        private static bool IsShared(string id)
        {
            return id != GetBluePrintTopTcmId(id) && id != GetBluePrintTopLocalizedTcmId(id);
        }

        public static void UnLocalize(MappingInfo mapping, string id)
        {
            if (!EnsureValidClient(mapping))
                return;

            UnLocalize(id);
        }

        private static void UnLocalize(string id)
        {
            if (IsLocalized(id))
                Client.UnLocalize(id, new ReadOptions());
        }

        public static void UnLocalizeAll(MappingInfo mapping, string id)
        {
            if (!EnsureValidClient(mapping))
                return;

            UnLocalizeAll(id);
        }

        private static void UnLocalizeAll(string id)
        {
            if (!IsAnyLocalized(id))
                return;

            var list = Client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = id } });
            if (list == null || list.Length == 0)
                return;

            var list2 = list.Cast<BluePrintNodeData>().Where(x => x.Item != null).ToList();

            foreach (BluePrintNodeData item in list2)
            {
                if (IsLocalized(item.Item.Id))
                    UnLocalize(item.Item.Id);
            }
        }

        #endregion

        #region Other

        public static string GetMimeType(string url)
        {
            string extension = Path.GetExtension(url);
            if (String.IsNullOrEmpty(extension))
                return String.Empty;

            if (Extensions.Any(x => x.Key == extension))
                return Extensions[extension];

            return String.Empty;
        }

        public static bool IsAllowedMimeType(string url)
        {
            return !String.IsNullOrEmpty(GetMimeType(url));
        }

        public static List<AssemblyShortInfo> GetAssemblies()
        {
            List<AssemblyShortInfo> res = new List<AssemblyShortInfo>();

            List<string> assemblyFolders = new List<string>
            { 
                @"C:\Windows\assembly",
                @"C:\Windows\Microsoft.NET\assembly"
            };
            
            List<string> gacFolders = new List<string>
            { 
                "GAC", "GAC_32", "GAC_64", "GAC_MSIL", 
                "NativeImages_v2.0.50727_32", 
                "NativeImages_v2.0.50727_64",
                "NativeImages_v4.0.30319_32",
                "NativeImages_v4.0.30319_64"
            };

            foreach (string folder1 in assemblyFolders)
            {
                foreach (string folder2 in gacFolders)
                {
                    string path = Path.Combine(folder1, folder2);
                    if (Directory.Exists(path))
                    {
                        foreach (string assemblyNamePath in Directory.GetDirectories(path))
                        {
                            string name = Path.GetFileName(assemblyNamePath);
                            if (String.IsNullOrEmpty(name))
                                continue;

                            foreach (string assemblyVersionPath in Directory.GetDirectories(assemblyNamePath))
                            {
                                //example v4.0_1.0.0.0__31bf3856ad364e35
                                string assemblyVersionName = Path.GetFileName(assemblyVersionPath);
                                if (String.IsNullOrEmpty(assemblyVersionName))
                                    continue;

                                string fwVersion = null;
                                string version = null;
                                string culture = null;
                                string token;

                                if (assemblyVersionName.Contains("_"))
                                {
                                    string[] arr = assemblyVersionName.Split('_');

                                    if (assemblyVersionName.StartsWith("v"))
                                    {
                                        fwVersion = arr[0].Replace("v", "");
                                        version = arr[1];
                                        culture = arr[2];
                                        token = arr[3];
                                    }
                                    else
                                    {
                                        version = arr[0];
                                        culture = arr[1];
                                        token = arr[2];
                                    }
                                }
                                else
                                {
                                    token = assemblyVersionName;
                                }

                                foreach (string assemblyPath in GetFiles(assemblyVersionPath, new[] { ".dll", ".exe" }))
                                {
                                    res.Add(new AssemblyShortInfo
                                    {
                                        Name = name,
                                        FrameworkVersion = fwVersion,
                                        Version = version,
                                        Culture = culture,
                                        Token = token,
                                        Path = assemblyPath
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return res;
        }

        private static List<AssemblyShortInfo> _allAssemblies;
        public static string GetAssembly(string name)
        {
            if (_allAssemblies == null)
                _allAssemblies = GetAssemblies();

            AssemblyShortInfo assembly = _allAssemblies.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
            if (assembly == null)
                return null;

            return assembly.FullName;
        }

        private static bool? _isTridionCM;
        public static bool IsTridionCM()
        {
            if (_isTridionCM == null)
            {
                string tridion = GetAssembly("Tridion.ContentManager");
                string razorMediator = GetAssembly("Tridion.Extensions.Mediators.Razor");

                _isTridionCM = !String.IsNullOrEmpty(tridion) && !String.IsNullOrEmpty(razorMediator);
            }
            return _isTridionCM == true;
        }

        #endregion

        #region Visual Studio

        public static Project GetConfiguredProject(Solution solution)
        {
            foreach (Project project in solution.Projects)
            {
                foreach (ProjectItem projectItem in project.ProjectItems)
                {
                    if (projectItem.Name.EndsWith("TridionRazorMapping.xml"))
                        return project;
                }
            }

            return null;
        }

        public static Project GetCurrentProject(DTE application)
        {
            foreach (SelectedItem selectedItem in application.SelectedItems)
            {
                return selectedItem.ProjectItem.ContainingProject;
            }

            return null;
        }

        public static Project GetProject(DTE application, Solution solution)
        {
            Project project = GetConfiguredProject(solution);
            if (project != null)
                return project;

            project = GetCurrentProject(application);
            if (project != null)
                return project;

            return solution.Projects.Item(1);
        }

        #endregion
    }
}