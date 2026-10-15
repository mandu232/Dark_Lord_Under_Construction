using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkLord.UI
{
    // Plain save names must be edited by committed string indices, not IME mesh indices.
    public sealed class SaveNameInputField : TMP_InputField
    {
        private readonly Event editingEvent = new Event();

        protected override void OnEnable()
        {
            base.OnEnable();
            richText = false;
            isRichTextEditingAllowed = true;
        }

        private EditState ProcessNameEvent(Event evt)
        {
            var module = EventSystem.current != null ? EventSystem.current.currentInputModule : null;
            string composition = module != null ? module.input.compositionString : Input.compositionString;
            // IME owns deletion and navigation within preedit text. Those positions do
            // not exist in TMP's committed string yet (including an empty string).
            if (!string.IsNullOrEmpty(composition) && evt.character == '\0' && evt.keyCode != KeyCode.Escape)
                return EditState.Continue;

            ClampStringPos(ref m_StringPosition);
            ClampStringPos(ref m_StringSelectPosition);
            var result = KeyPressed(evt);
            ForceLabelUpdate();
            return result;
        }

        public new void ProcessEvent(Event evt) => ProcessNameEvent(evt);

        public override void OnUpdateSelected(BaseEventData eventData)
        {
            if (!isFocused) return;
            while (Event.PopEvent(editingEvent))
            {
                if (editingEvent.rawType == EventType.KeyDown)
                {
                    if (ProcessNameEvent(editingEvent) == EditState.Finish)
                    {
                        if (editingEvent.keyCode != KeyCode.Escape) SendOnSubmit();
                        DeactivateInputField();
                        break;
                    }
                }
                else if ((editingEvent.rawType == EventType.ValidateCommand || editingEvent.rawType == EventType.ExecuteCommand) && editingEvent.commandName == "SelectAll")
                    ProcessNameEvent(Event.KeyboardEvent("^a"));
            }
            ForceLabelUpdate();
            eventData.Use();
        }
    }
}
