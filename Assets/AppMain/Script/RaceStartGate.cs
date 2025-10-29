using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RaceStartGate : MonoBehaviour
{
    [Header("GO�ŉ��ւ������R���|�[�l���g�i�ԑ���n�j")]
    // ��FPrometeoCarController �ȂǁA������i��X�N���v�g�����Ă�������
    public MonoBehaviour[] controlScripts;

    [Header("�C�ӁFInput System ���g���Ă���ꍇ")]
#if ENABLE_INPUT_SYSTEM
    public PlayerInput playerInput;  // Player �̃I�u�W�F�N�g�ɂ��Ă��� PlayerInput �����蓖��
#endif

    [Header("��~�̈��艻�i�C�Ӂj")]
    public Rigidbody rb;             // ���蓖�Ă�ƁA���b�N���ɑ��x��0�ɂ��܂�
    public bool applyFullBrakeWhileLocked = true;

    bool isLocked = true;

    void Awake()
    {
        LockControls();
    }

    // �J�E���g�_�E���J�n���E�V�[���J�n���ɌĂԁiAwake�ł��łɌĂ�ł��܂��j
    public void LockControls()
    {
        isLocked = true;

        foreach (var s in controlScripts)
            if (s) s.enabled = false;

#if ENABLE_INPUT_SYSTEM
        if (playerInput) playerInput.DeactivateInput();
#endif

        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // �uGO!�v�̏u�ԂɌĂ�
    public void UnlockControls()
    {
        if (!isLocked) return;
        isLocked = false;

#if ENABLE_INPUT_SYSTEM
        if (playerInput) playerInput.ActivateInput();
#endif

        foreach (var s in controlScripts)
            if (s) s.enabled = true;
    }

    // �����ԑ���Update�Ŗ\������Ȃ�A�����Ŋ��S�u���b�N���ł���
    // public bool IsLocked() => isLocked;
}
