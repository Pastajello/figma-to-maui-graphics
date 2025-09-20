// Authors:
//   Jose Medrano <josmed@microsoft.com>
//
// Copyright (C) 2018 Microsoft, Corp
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to permit
// persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
// NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
// USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FigmaSharp.Helpers;
using FigmaSharp.Models;

namespace FigmaSharp.Services
{
    public abstract class NodeProvider : INodeProvider
    {
        public event EventHandler ImageLinksProcessed;

        public FigmaFileResponse Response { get; protected set; }
        public List<FigmaNode> Nodes { get; } = new List<FigmaNode>();

        public bool ImageProcessed;

        internal void OnImageLinkProcessed()
        {
            ImageProcessed = true;
            ImageLinksProcessed?.Invoke(this, new EventArgs());
        }

        public string FigmaFileId { get; set; }

        public Task LoadAsync(string file) => Load(file);
        public Task LoadAsync(string file, string nodeId,FigmaNode figmaNode, int depth) => LoadWithNodeId(file, nodeId,figmaNode, depth);

        private async Task LoadWithNodeId(string file, string nodeId,FigmaNode node, int depth)
        {
            this.FigmaFileId = file;

            ImageProcessed = false;
            try
            {
                Console.WriteLine($"LoadWithNodeId");
                var contentTemplate = await GetContentById(file, nodeId, depth);
                //parse the json into a model format
                var response = WebApiHelper.GetFigmaResponseFromNodeContent(contentTemplate);

                //proceses all the views recursively
                foreach (var bigNode in response.nodes)
                {
                    foreach (var child in bigNode.Value.document.children)
                    {
                        if (node is IFigmaNodeContainer container)
                        {
                            container.children.Add(child);
                        }
                        ProcessNodeRecursively(child, node);
                    }
                }

                await LoadImages();
            }
            catch (System.Net.WebException ex)
            {
                if (!AppContext.Current.IsApiConfigured)
                    LoggingService.LogError($"Cannot connect to Figma server: TOKEN not configured.", ex);
                else
                    LoggingService.LogError($"Cannot connect to Figma server: wrong TOKEN?", ex);
            }
            catch (Exception ex)
            {
                LoggingService.LogError(
                    $"Error reading remote resources. Ensure you added NewtonSoft nuget or cannot parse the to json?",
                    ex);
            }
        }

        public async Task Load(string file)
        {
            this.FigmaFileId = file;

            ImageProcessed = false;
            try
            {
                Nodes.Clear();

                var contentTemplate = await GetContentTemplate(file);

                //parse the json into a model format
                Response = WebApiHelper.GetFigmaResponseFromFileContent(contentTemplate);

                //proceses all the views recursively
                foreach (var item in Response.document.children)
                    ProcessNodeRecursively(item, null);

                await LoadImages();
            }
            catch (System.Net.WebException ex)
            {
                if (!AppContext.Current.IsApiConfigured)
                    LoggingService.LogError($"Cannot connect to Figma server: TOKEN not configured.", ex);
                else
                    LoggingService.LogError($"Cannot connect to Figma server: wrong TOKEN?", ex);
            }
            catch (Exception ex)
            {
                LoggingService.LogError(
                    $"Error reading remote resources. Ensure you added NewtonSoft nuget or cannot parse the to json?",
                    ex);
            }
        }

        private async Task LoadImages()
        {
            var watch = Stopwatch.StartNew();

            var images = SearchImageNodes();
            var imageRequests = new List<IImageNodeRequest>();
            if (images == null || images.Count() < 1)
            {
                return;
            }

            foreach (var image in images)
            {
                var imageRequest = CreateEmptyImageNodeRequest(image);
                imageRequests.Add(imageRequest);
            }

            watch.Stop();


            var milis = watch.ElapsedMilliseconds;
            Console.WriteLine($"Elapsed on prepare: {milis}");
            watch.Restart();
            await AppContext.Api.ProcessDownloadImagesAsync(FigmaFileId, imageRequests.ToArray());
            watch.Stop();
            milis = watch.ElapsedMilliseconds;
            Console.WriteLine($"Elapsed on download: {milis}");
            watch.Restart();
            await SaveResourceFilesAsync("images", ".png", imageRequests.ToArray());
            watch.Stop();
            milis = watch.ElapsedMilliseconds;
            Console.WriteLine($"Elapsed on save: {milis}");
        }

        public FigmaNode[] GetMainGeneratedLayers()
        {
            return GetMainLayers(s =>
                    s.TryGetNodeCustomName(out var customName) && !s.name.StartsWith("#") && !s.name.StartsWith("//"))
                .ToArray();
        }

        public IEnumerable<FigmaNode> GetMainLayers(Func<FigmaNode, bool> action = null)
        {
            return Nodes.Where(s => s.Parent is FigmaCanvas && (action?.Invoke(s) ?? true));
        }

        public FigmaNode FindByFullPath(string fullPath)
        {
            return FindByPath(fullPath.Split('/'));
        }

        /// <summary>
        /// Finds a node using the path of the views, returns null in case of no data
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public FigmaNode FindByPath(params string[] path)
        {
            if (path.Length == 0)
            {
                return null;
            }

            FigmaNode figmaNode = null;
            for (int i = 0; i < path.Length; i++)
            {
                if (i == 0)
                    figmaNode = Nodes.FirstOrDefault(s => s.name == path[i]);
                else
                    figmaNode = Nodes.FirstOrDefault(s => s.name == path[i] && s.Parent.id == figmaNode.id);

                if (figmaNode == null)
                    return null;
            }

            return figmaNode;
        }

        public FigmaNode FindByName(string name)
        {
            var quotedName = string.Format("\"{0}\"", name);
            var found = Nodes.FirstOrDefault(s => s.name.Contains(quotedName));
            if (found != null)
            {
                return found;
            }

            return Nodes.FirstOrDefault(s => s.name == name);
        }

        // void ProcessNodeRecursively(Node node, Node parent)
        // {
        //     node.Parent = parent;
        //     Nodes.Add(node);
        //
        //     if (node is FigmaInstance instance)
        //     {
        //         if (Response.components.TryGetValue(instance.componentId, out var figmaComponent))
        //             instance.Component = figmaComponent;
        //     }
        //
        //     if (node is IFigmaNodeContainer nodeContainer)
        //     {
        //         foreach (var item in nodeContainer.children)
        //             ProcessNodeRecursively(item, node);
        //     }
        // }


        void ProcessNodeRecursively(FigmaNode node, FigmaNode parent)
        {
            node.Parent = parent;
            Nodes.Add(node);

            if (node is FigmaInstance instance)
            {
                if (Response.components.TryGetValue(instance.componentId, out var figmaComponent))
                    instance.Component = figmaComponent;
            }

            if (node is IFigmaNodeContainer nodeContainer)
            {
                foreach (var item in nodeContainer.children)
                    ProcessNodeRecursively(item, node);
            }
        }

        public abstract Task<string> GetContentTemplate(string file);
        public abstract Task<string> GetContentById(string file, string id, int depth);

        public abstract void OnStartImageLinkProcessing(List<ViewNode> imageFigmaNodes);

        public void Save(string filePath)
        {
            Response.Save(filePath);
        }

        public bool TryGetMainInstance(FigmaInstance nodeInstance, out FigmaInstance result)
        {
            //Get the instance
            var componentNode = GetMainGeneratedLayers();
            foreach (var item in componentNode)
            {
                if (item is FigmaInstance figmaInstance && figmaInstance.id == nodeInstance.componentId)
                {
                    result = figmaInstance;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public bool TryGetMainComponent(FigmaInstance nodeInstance, out FigmaComponentEntity result)
        {
            //Get the instance
            var componentNode = GetMainGeneratedLayers();
            foreach (var item in componentNode)
            {
                if (item is FigmaComponentEntity figmaInstance && figmaInstance.id == nodeInstance.componentId)
                {
                    result = figmaInstance;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public bool TryGetStyle(string fillStyleValue, out FigmaStyle style)
        {
            return Response.styles.TryGetValue(fillStyleValue, out style);
        }

        #region Image Resources

        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task SaveResourceFilesAsync(
            string destinationDirectory,
            string format,
            IImageNodeRequest[] downloadImages)
        {
            if (!Directory.Exists(destinationDirectory))
            {
                var destDir = Directory.CreateDirectory(destinationDirectory);
                Console.WriteLine(destDir.FullName);
            }

            var downloadTasks = new List<Task>();

            foreach (var downloadImage in downloadImages)
            {
                foreach (var imageScale in downloadImage.Scales)
                {
                    if (string.IsNullOrEmpty(imageScale.Url))
                        continue;

                    string customNodeName = downloadImage.GetOutputFileName(imageScale.Scale);
                    var fileName = string.Concat(customNodeName, format);
                    var safeFileName = $"image_{fileName.Replace(":", "_").Replace(";", "_")}";
                    var fullPath = Path.Combine(destinationDirectory, safeFileName);

                    // if file exists - lucky, go to the nexy
                    if (File.Exists(fullPath))
                    {
                        continue;
                    }

                    downloadTasks.Add(DownloadFileAsync(imageScale.Url, fullPath));
                }
            }

            await Task.WhenAll(downloadTasks);
        }

        private static async Task DownloadFileAsync(string url, string destinationPath)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = File.Create(destinationPath);

                await stream.CopyToAsync(fileStream);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("[FIGMA] Error while downloading file.", ex);
            }
        }

        public virtual bool RendersAsImage(FigmaNode figmaNode)
        {
            if (figmaNode.ContainsSourceImage())
                return true;

            if (figmaNode is IFigmaImage figmaImage && figmaImage.HasImage())
                return true;

            return false;
        }

        public virtual bool SearchImageChildren(FigmaNode figmaNode) => true;

        public IEnumerable<FigmaNode> SearchImageNodes(FigmaNode mainNode)
        {
            if (RendersAsImage(mainNode))
            {
                yield return mainNode;
                yield break;
            }

            //we don't want iterate on children
            if (!SearchImageChildren(mainNode))
                yield break;

            if (mainNode is IFigmaNodeContainer nodeContainer)
            {
                foreach (var item in nodeContainer.children)
                {
                    foreach (var resultItems in SearchImageNodes(item))
                    {
                        yield return resultItems;
                    }
                }
            }
            else if (mainNode is FigmaDocument document)
            {
                foreach (var item in document.children)
                {
                    foreach (var resultItems in SearchImageNodes(item))
                    {
                        yield return resultItems;
                    }
                }
            }
        }

        public IEnumerable<FigmaNode> SearchImageNodes() => SearchImageNodes(Response.document);

        public virtual IImageNodeRequest CreateEmptyImageNodeRequest(FigmaNode node) => new ImageNodeRequest(node);

        #endregion
    }
}