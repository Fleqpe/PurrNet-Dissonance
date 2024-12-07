using PurrNet;
using PurrNet.Logging;
using PurrNet.Utils;
using UnityEngine;

namespace Dissonance.Integrations.PurrNet
{
    public class PurrNetDissonancePlayer : NetworkIdentity, IDissonancePlayer
    {
        [Tooltip("If this is not set, the transform of this object will be used.")] [SerializeField]
        private Transform trackingTransform;

#if UNITY_EDITOR
        [SerializeField, PurrReadOnly] private string dissonanceId_Debug;
        [SerializeField, PurrReadOnly] private bool isTracking_Debug;
#endif

        private readonly SyncVar<string> _playerId = new("", ownerAuth: true);

        public DissonanceComms Comms { get; private set; }

        private Transform _transform;
        private PurrNetCommsNetwork _purrComms;

        public string PlayerId => _localPlayerId;
        public Vector3 Position => _transform.position;
        public Quaternion Rotation => _transform.rotation;
        public NetworkPlayerType Type => isOwner ? NetworkPlayerType.Local : NetworkPlayerType.Remote;
        public bool IsTracking { get; private set; }
        private string _localPlayerId = "";

        private void Awake()
        {
            _transform = trackingTransform ?? transform;
            _playerId.onChanged += OnPlayerIdChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _playerId.onChanged -= OnPlayerIdChanged;
        }

        private void OnPlayerIdChanged(string obj)
        {
            if (IsTracking)
                ManageTrackingState(false);

#if UNITY_EDITOR
            dissonanceId_Debug = obj;
#endif

            _localPlayerId = obj;
            ManageTrackingState(true);
        }

        private void OnEnable()
        {
            ManageTrackingState(true);
        }

        private void OnDisable()
        {
            ManageTrackingState(false);
        }

        protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
        {
            base.OnOwnerChanged(oldOwner, newOwner, asServer);

            if (!isOwner)
                return;

            if (!_purrComms)
            {
                PurrLogger.LogError($"Dissonance player couldn't find PurrNetCommsNetwork instance.");
                return;
            }

            if (newOwner.HasValue)
            {
                _playerId.value = newOwner.Value.id.ToString();
                ManageTrackingState(true);
            }
            else
            {
                _playerId.value = "";
                ManageTrackingState(false);
            }
        }

        private void ManageTrackingState(bool track)
        {
            if (IsTracking == track) return;
            if (!InstanceHandler.TryGetInstance(out _purrComms))
            {
                //PurrLogger.LogError($"PurrNetCommsNetwork instance not found.");
                return;
            }

            if (track && !_purrComms.IsInitialized)
            {
                PurrLogger.LogWarning($"PurrNetDissonancePlayer is not yet initialized, so we can't start tracking");
                return;
            }

            DissonanceComms comms = _purrComms.comms;
            if (track)
                comms.TrackPlayerPosition(this);
            else
                comms.StopTracking(this);

            IsTracking = track;

#if UNITY_EDITOR
            isTracking_Debug = IsTracking;
#endif
        }
    }
}
