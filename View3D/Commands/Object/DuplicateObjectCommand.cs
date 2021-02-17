﻿using Common;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Rendering;
using View3D.SceneNodes;

namespace View3D.Commands.Object
{
    public class DuplicateObjectCommand : CommandBase<DuplicateObjectCommand>
    {
        List<SceneNode> _objectsToCopy;
        List<SceneNode> _clonedObjects = new List<SceneNode>();
        SceneManager _sceneManager;
        SelectionManager _selectionManager;

        ISelectionState _oldState;
        public DuplicateObjectCommand(List<SceneNode> objectsToCopy)
        {
            _objectsToCopy = new List<SceneNode>(objectsToCopy);
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _sceneManager = componentManager.GetComponent<SceneManager>();
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
            _logger.Here().Information($"Command info - Items[{string.Join(',', _objectsToCopy.Select(x => x.Name))}]");

            _oldState = _selectionManager.GetStateCopy();

            var state = _selectionManager.CreateSelectionSate(GeometrySelectionMode.Object);
            var objectState = state as ObjectSelectionState;

            foreach (var item in _objectsToCopy)
            {
                var clonedItem = item.Clone();
                clonedItem.Id = Guid.NewGuid().ToString();
                _clonedObjects.Add(clonedItem);
                item.Parent.AddObject(clonedItem);
                if(clonedItem is ISelectable selectableNode)
                    objectState.ModifySelection(selectableNode);
            }
        }

        protected override void UndoCommand()
        {
            foreach (var item in _clonedObjects)
                item.Parent.RemoveObject(item);

            _selectionManager.SetState(_oldState);
        }
    }
}
