using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace UIKit
{
    public interface UIKPlayerManager
    {
        public UnityEvent<UIKPlayer> OnPlayerJoined { get; set; }
        public UnityEvent<UIKPlayer> OnPlayerLeft { get; set; }
        
        public List<UIKPlayer> players { get; set; }
        
        /// <summary>
        /// This must be set manually in your project's code
        /// </summary>
        public static UIKPlayerManager instance;

        public static bool routeInputWhenUnfocused;

        
        public List<UIKPlayer> localPlayers => players.Where(p => p.GetIsLocal()).ToList();
        
        /// <summary>
        /// Have this function return the result of UIKPlayerManager.JoinPlayer(_playerManager, _playerIndex, _splitScreenIndex, _controlScheme, _inputDevices)
        /// </summary>
        public UIKPlayer JoinPlayer(int _playerIndex = -1, int _splitScreenIndex = -1, string _controlScheme = null, InputDevice[] _inputDevices = null);

        public static UIKPlayer JoinPlayer(UIKPlayerManager _playerManager, int _playerIndex, int _splitScreenIndex, string _controlScheme, InputDevice[] _inputDevices = null)
        {
            if (PlayerInputManager.instance == null)
            {
                return null;
            }
            
            PlayerInput playerInput = PlayerInputManager.instance.JoinPlayer(_playerIndex, _splitScreenIndex, _controlScheme, _inputDevices);
            if (playerInput.GetComponent<UIKPlayer>() is not UIKPlayer player)
            {
                return null;
            }
            
            player.playerInput = playerInput;
            
            _playerManager.players.Add(player);
            
            // Initialize the player
            MonoBehaviour.DontDestroyOnLoad(playerInput.gameObject);
            playerInput.onActionTriggered += PlayerInput_OnActionTriggered;
            playerInput.onControlsChanged += PlayerInput_OnControlsChanged;
            PlayerInput_OnControlsChanged(playerInput);
            
            player.OnPrePlayerJoined();
            _playerManager.OnPlayerJoined?.Invoke(player);
            
            return player;
        }

        /// <summary>
        /// Have this function call UIKPlayerManager.LeavePlayer(_playerManager, _player)
        /// </summary>
        public void LeavePlayer(UIKPlayer _player);

        public static void LeavePlayer(UIKPlayerManager _playerManager, UIKPlayer _player)
        {
            _player.OnPrePlayerLeft();
            
            // We want the player removed from our player list after we call PrePlayerLeft, but before OnPlayerLeft
            _playerManager.players.Remove(_player);
            
            _playerManager.OnPlayerLeft?.Invoke(_player);
            
            if (_player is MonoBehaviour monoBehaviour)
            {
                MonoBehaviour.Destroy(monoBehaviour.gameObject);
            }
        }
        
        private static void PlayerInput_OnActionTriggered(InputAction.CallbackContext _context)
        {
            // The device used to trigger the action
            // _context.action.activeControl.device

            if ((!Application.isFocused && !routeInputWhenUnfocused)
                || UIKInputBlockers.isBlocked)
            {
                return;
            }
            
            // For all players using the device that the input action was triggered on
            foreach (UIKPlayer player in instance.players.Where(p => p.GetInputDevices().Contains(_context.action.activeControl.device)).ToArray())
            {
                // Don't allow inputs that aren't in the current action map to pass
                if (_context.action.actionMap != null
                    && player.playerInput.currentActionMap != null
                    && _context.action.actionMap.name != player.playerInput.currentActionMap.name)
                {
                    continue;
                }
                
                // Canvas gets first dibs on consuming input actions, if we have one
                if (player.canvas)
                {
                    if (player.canvas.HandleInputAction(_context))
                    {
                        continue; // If we returned true, don't do any further input handling
                    }
                }
                
                // Player gets second dibs on consuming input actions
                if (player.HandleInputAction(_context))
                {
                    continue; // If we returned true, don't do any further input handling
                }
                
                // Broadcast the input for anyone to listen to
                player.OnInputActionTriggered.Invoke(_context);
            }
        }
        
        private static void PlayerInput_OnControlsChanged(PlayerInput _playerInput)
        {
            if (instance.players.FirstOrDefault(p => p.playerInput == _playerInput) is UIKPlayer player)
            {
                UIKPlayer.OnControlsChanged(player, _playerInput);
            }
        }
    }
} // UIKit namespace
