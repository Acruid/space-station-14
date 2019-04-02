﻿using System.Collections.Generic;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameStates;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network.Messages;
using SS14.Client.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.Timing;
using SS14.Shared.Utility;

namespace SS14.Client.GameStates
{
    public class ClientGameStateManager : IClientGameStateManager
    {
        private readonly List<GameState> _stateBuffer = new List<GameState>();
        private GameState _lastFullState;
        private bool _waitingForFull = true;
        
        [Dependency]
        private readonly IClientEntityManager _entities;
        [Dependency]
        private readonly IPlayerManager _players;
        [Dependency]
        private readonly IClientNetManager _network;
        [Dependency]
        private readonly IBaseClient _client;
        [Dependency]
        private readonly IMapManager _mapManager;
        [Dependency]
        private readonly IGameTiming _timing;

        public void Initialize()
        {
            _network.RegisterNetMessage<MsgState>(MsgState.NAME, HandleStateMessage);
            _network.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME);
            _client.RunLevelChanged += RunLevelChanged;
        }

        private void RunLevelChanged(object sender, RunLevelChangedEventArgs args)
        {
            if (args.NewLevel == ClientRunLevel.Initialize)
            {
                // We JUST left a server or the client started up, Reset everything.
                _stateBuffer.Clear();
            }
        }

        private void HandleStateMessage(MsgState message)
        {
            var state = message.State;

            // we always ack everything we receive, even if it is late
            AckGameState(state.ToSequence);

            // any state from tick 0 is a full state, and needs to be handled different
            if (state.FromSequence == GameTick.Zero)
            {
                // this is a newer full state, so discard the older one.
                if(_lastFullState == null || (_lastFullState != null && _lastFullState.ToSequence < state.ToSequence))
                {
                    _lastFullState = state;
                    Logger.InfoS("net", $"Received Full GameState: to={state.ToSequence}, sz={0}");
                    return;
                }
            }

            // NOTE: DispatchTick will be modifying CurTick, this is NOT thread safe.
            var lastTick = new GameTick(_timing.CurTick.Value - 1);

            if (state.ToSequence <= lastTick && !_waitingForFull) // CurTick isn't set properly when WaitingForFull
            {
                Logger.DebugS("net.state", $"Received Old GameState: cTick={_timing.CurTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={0}, buf={_stateBuffer.Count}");
                return;
            }
            
            for (var i = 0; i < _stateBuffer.Count; i++)
            {
                var iState = _stateBuffer[i];

                if (state.ToSequence != iState.ToSequence)
                    continue;

                if (iState.Extrapolated)
                {
                    _stateBuffer.RemoveSwap(i); // remove the fake extrapolated state
                    break; // break from the loop, add the new state normally
                }

                Logger.DebugS("net.state", $"Received Dupe GameState: cTick={_timing.CurTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={0}, buf={_stateBuffer.Count}");
                return;
            }
            
            _stateBuffer.Add(state);
            Logger.DebugS("net.state", $"Received New GameState: cTick={_timing.CurTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={0}, buf={_stateBuffer.Count}");
        }

        public void ApplyGameState()
        {
            //TODO: completely skip this tick and freeze the sim if false
            if(CalculateNextStates(_timing.CurTick, out var curState, out var nextState))
            {
                Logger.DebugS("net.state", $"Applying State: cTick={_timing.CurTick}, ext={curState.Extrapolated} fSeq={curState.FromSequence}, tSeq={curState.ToSequence}, sz={0}, buf={_stateBuffer.Count}");
                ApplyGameState(curState);
            }
        }

        private bool CalculateNextStates(GameTick curTick, out GameState curState, out GameState nextState)
        {
            if (_waitingForFull)
            {
                if (_lastFullState != null)
                {
                    Logger.DebugS("net", $"Resync CurTick to: {_lastFullState.ToSequence}");
                    curTick = _timing.CurTick = _lastFullState.ToSequence;
                }

                var nextTick = new GameTick(curTick.Value + 1);

                // we are waiting to get a full state, and the next state
                if (_lastFullState != null)
                {
                    nextState = null;
                    for (var i = 0; i < _stateBuffer.Count; i++)
                    {
                        var state = _stateBuffer[i];
                        if (state.ToSequence == nextTick)
                        {
                            nextState = state;
                            break;
                        }
                    }

                    if (nextState != null)
                    {
                        curState = _lastFullState;
                        _waitingForFull = false;
                        return true;
                    }
                }

                // We just have to wait...
                curState = default;
                nextState = default;
                return false;
            }
            else // this will be how almost all states are calculated
            {
                var lastTick = new GameTick(curTick.Value - 1);
                var nextTick = new GameTick(curTick.Value + 1);

                curState = null;
                nextState = null;

                for (var i = 0; i < _stateBuffer.Count; i++)
                {
                    var state = _stateBuffer[i];

                    if (state.FromSequence == lastTick && state.ToSequence == curTick)
                    {
                        curState = state;
                        _stateBuffer.RemoveSwap(i);
                        i--;
                    }
                    else if (state.FromSequence == curTick && state.ToSequence == nextTick)
                    {
                        nextState = state;
                        _stateBuffer.RemoveSwap(i);
                        i--;
                    }
                    else if(state.ToSequence < lastTick) // remove any old states we find to keep the buffer clean
                    {
                        _stateBuffer.RemoveSwap(i);
                        i--;
                    }
                }

                // we found both the states to lerp between, this should be true almost always.
                if (curState != null && nextState != null)
                    return true;

                if (curState == null)
                {
                    curState = ExtrapolateState(lastTick, curTick);
                    _stateBuffer.Add(curState);
                }

                if (nextState == null)
                {
                    nextState = ExtrapolateState(curTick, nextTick);
                    _stateBuffer.Add(nextState);
                }

                return true;
            }
        }

        private GameState ExtrapolateState(GameTick fromSequence, GameTick toSequence)
        {
            //TODO: Make me work!
           var state = new GameState(fromSequence, toSequence, null, null, null, null);
           state.Extrapolated = true;
           return state;
        }

        private void AckGameState(GameTick sequence)
        {
            var msg = _network.CreateNetMessage<MsgStateAck>();
            msg.Sequence = sequence;
            _network.ClientSendMessage(msg);
        }

        private void ApplyGameState(GameState gameState)
        {
            _mapManager.ApplyGameStatePre(gameState.MapData);
            _entities.ApplyEntityStates(gameState.EntityStates, gameState.EntityDeletions);
            _players.ApplyPlayerStates(gameState.PlayerStates);
            _mapManager.ApplyGameStatePost(gameState.MapData);
        }
    }
}
