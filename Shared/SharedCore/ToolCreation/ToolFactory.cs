﻿using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Shared.Core.DependencyInjection;
using Shared.Core.ErrorHandling;

namespace Shared.Core.ToolCreation
{

    public record EditorInfo(EditorEnums EditorEnum, Type View, Type ViewModel, IPackFileToToolResolver ExtentionHandler)
   {
        public static EditorInfo Create<TViewModel, TView>(EditorEnums editorEnum, IPackFileToToolResolver extentionHandler)
           where TViewModel : IEditorViewModel
           where TView : Control
        {
            return new EditorInfo(editorEnum, typeof(TView), typeof(TViewModel), extentionHandler); 
        }
    }

    public interface IToolFactory
    {
        public void Register(EditorInfo editorInfo);

        IEditorViewModel Create(string fullFileName, EditorEnums? preferedEditor = null);
        IEditorViewModel Create(EditorEnums editorEnum);

        void DestroyEditor(IEditorViewModel instance);
        Type GetViewTypeFromViewModel(Type viewModelType);
    }

    public class ToolFactory : IToolFactory
    {
        private readonly ILogger _logger = Logging.Create<ToolFactory>();

        private readonly IServiceProvider _serviceProvider;
        private readonly ScopeRepository _scopeRepository;
        private readonly IToolSelectorUiProvider _toolSelectorUiProvider;

        private readonly List<EditorInfo> _editors = [];

        public ToolFactory(IServiceProvider serviceProvider, ScopeRepository scopeRepository, IToolSelectorUiProvider toolSelectorUiProvider)
        {
            _serviceProvider = serviceProvider;
            _scopeRepository = scopeRepository;
            _toolSelectorUiProvider = toolSelectorUiProvider;
        }

        public void Register(EditorInfo editorInfo)
        {
            if (_editors.Any(x => x.EditorEnum == editorInfo.EditorEnum))
            {
                var errorMessage = $"Tool already registered - {editorInfo.EditorEnum}";
                _logger.Here().Error(errorMessage);
                throw new Exception(errorMessage);
            }

            _editors.Add(editorInfo);
        }

        public Type GetViewTypeFromViewModel(Type viewModelType)
        {
            _logger.Here().Information($"Getting view for ViewModel - {viewModelType}");
            
            var instance = _editors.First(x=>x.ViewModel ==  viewModelType);
            return instance.View;
        }

        public IEditorViewModel Create(EditorEnums editorEnum) 
        {
            var editor = _editors.First(x => x.EditorEnum == editorEnum);
            return CreateEditorInternal(editor.ViewModel);
        }

        public IEditorViewModel Create(string fullFileName, EditorEnums? preferedEditor)
        {
            var allEditors = GetAllPossibleEditors(fullFileName);

            if (allEditors.Count == 0)
            {
                _logger.Here().Warning($"Trying to open file {fullFileName}, but there are no valid tools for it.");
                return null;
            }

            Type selectedEditor = null;
            if (allEditors.Count == 1)
            {
                selectedEditor = allEditors.First().ViewModel;
            }
            else if (allEditors.Count > 1 && preferedEditor != null)
            {
                var preferedEditorType = allEditors.FirstOrDefault(x => x.EditorEnum == preferedEditor);
                if(preferedEditorType == null)
                    throw new Exception($"The prefered editor {preferedEditor} can not open {fullFileName}");
                selectedEditor = preferedEditorType.ViewModel;
            }
            else
            {
                var selectedToolType = _toolSelectorUiProvider.CreateAndShow(allEditors.Select(x => x.EditorEnum));
                if (selectedToolType == EditorEnums.None)
                    return null;
                selectedEditor = allEditors.First(x => x.EditorEnum == selectedToolType).ViewModel;
            }

            return CreateEditorInternal(selectedEditor);
        }

        IEditorViewModel CreateEditorInternal(Type editorType)
        {
            var scope = _serviceProvider.CreateScope();
            var instance = scope.ServiceProvider.GetRequiredService(editorType) as IEditorViewModel;
            if (instance == null)
                throw new Exception($"Type '{editorType}' is not a IEditorViewModel");
            _scopeRepository.Add(instance, scope);
            return instance;
        }

        List<EditorInfo> GetAllPossibleEditors(string filename)
        {
            var output = new List<EditorInfo>();
            foreach (var toolLookUp in _editors)
            {
                var result = toolLookUp.ExtentionHandler.CanOpen(filename);
                if (result.CanOpen)
                    output.Add(toolLookUp);
            }

            if (output.Count == 0)
            {
                var error = $"Attempting to get view model for file {filename}, unable to find tool based on extension";
                _logger.Here().Error(error);
            }

            return output;
        }

        public void DestroyEditor(IEditorViewModel instance) => _scopeRepository.RemoveScope(instance);
    }
}
