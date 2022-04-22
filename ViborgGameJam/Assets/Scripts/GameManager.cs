using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Users;

public partial class GameManager : SystemBase {
    private InputAction _inputAction;
    private Controller global;
    protected override void OnStartRunning() {
        global = new Controller();
        global.Enable();

        var playerPrefab = GetSingleton<PlayerReference>().Value;

        InputUser.listenForUnpairedDeviceActivity = 2;
        InputUser.onUnpairedDeviceUsed += (control, ptr) => {
            if (!(control is ButtonControl))
                return;
            var user = InputUser.PerformPairingWithDevice(control.device);
            var player = new Controller();
            player.Enable();
            user.AssociateActionsWithUser(player);

            EntityManager.Instantiate(playerPrefab);
        };
    }

    protected override void OnUpdate() {
        
    }
}