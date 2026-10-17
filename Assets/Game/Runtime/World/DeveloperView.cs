using Game.Core;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>
    /// The player's character at the desk (PLAN 4.3). Types while a game is in development and rests
    /// otherwise; the monitor is on while working. The animation stops while the game is paused.
    /// </summary>
    public sealed class DeveloperView : MonoBehaviour
    {
        [SerializeField] private GameRunner runner;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Sprite[] workFrames = new Sprite[0];
        [SerializeField] private Sprite restSprite;
        [Header("Desk monitor")]
        [SerializeField] private SpriteRenderer desk;
        [SerializeField] private Sprite deskScreenOff;
        [SerializeField] private Sprite deskScreenOn;
        [SerializeField, Min(0.05f)] private float frameSeconds = 0.25f;

        private float _frameTime;
        private int _frame;

        public DeveloperActivity Activity { get; private set; }
        public Sprite CurrentSprite => body != null ? body.sprite : null;

        private void Start() => Apply(DeveloperActivityRule.For(runner != null ? runner.Simulation : null), force: true);

        private void Update()
        {
            var simulation = runner != null ? runner.Simulation : null;
            Apply(DeveloperActivityRule.For(simulation), force: false);
            if (Activity != DeveloperActivity.Working || workFrames.Length < 2) return;
            if (runner.CurrentSpeed == GameRunner.Speed.Paused) return;

            _frameTime += Time.unscaledDeltaTime;
            if (_frameTime < frameSeconds) return;
            _frameTime = 0f;
            _frame = (_frame + 1) % workFrames.Length;
            body.sprite = workFrames[_frame];
        }

        private void Apply(DeveloperActivity activity, bool force)
        {
            if (!force && activity == Activity) return;
            Activity = activity;
            _frame = 0;
            _frameTime = 0f;
            bool working = activity == DeveloperActivity.Working;
            if (body != null) body.sprite = working && workFrames.Length > 0 ? workFrames[0] : restSprite;
            if (desk != null) desk.sprite = working ? deskScreenOn : deskScreenOff;
        }

        /// <summary>Wires the view in code; used by the scene builder.</summary>
        public void Configure(GameRunner gameRunner, SpriteRenderer bodyRenderer, Sprite[] work, Sprite rest,
            SpriteRenderer deskRenderer, Sprite screenOff, Sprite screenOn)
        {
            runner = gameRunner;
            body = bodyRenderer;
            workFrames = work ?? new Sprite[0];
            restSprite = rest;
            desk = deskRenderer;
            deskScreenOff = screenOff;
            deskScreenOn = screenOn;
        }
    }
}
