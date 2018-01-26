
using Battlehub.UIControls;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class GameResultsPanel : ButtonsPanel
    {
        private const int SaveReplayAction = 1;

        [SerializeField]
        private Text m_resultTxt;

        [SerializeField]
        private ButtonsPanel m_saveReplayPanel;

        private IVoxelGame m_gameState;

        public string ResultText
        {
            get { return m_resultTxt.text; }
            set { m_resultTxt.text = value; }
        }

        public override bool IsOpened
        {
            get { return base.IsOpened; }
            set
            {
                base.IsOpened = value;

                m_gameState = Dependencies.GameState;
                Sequence[SaveReplayAction].interactable = !m_gameState.IsReplay;
                Sequence[SaveReplayAction].gameObject.SetActive(!m_gameState.IsReplay);

                m_saveReplayPanel.IsOpened = false;
            }
        }

        private void Awake()
        {
            
            m_saveReplayPanel.Action += OnSaveReplayAction;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if(m_saveReplayPanel != null)
            {
                m_saveReplayPanel.Action -= OnSaveReplayAction;
            }
        }

        private void OnSaveReplayAction(ButtonsPanel sender, int code)
        {
            m_saveReplayPanel.IsOpened = false;
            gameObject.SetActive(true);

            IndependentSelectable.Select(m_sequence[0]);
        }

        protected override void OnAction(int index)
        {
            if(index == SaveReplayAction)
            {
                m_saveReplayPanel.LocalPlayerIndex = LocalPlayerIndex;
                m_saveReplayPanel.IsOpened = true;
                gameObject.SetActive(false);
            }
            else
            {
                base.OnAction(index);
            }
        }
    }

}
