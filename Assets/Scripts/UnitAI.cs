using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team
{
    Player,
    Enemy,
    Neutral // optional if you want non-fighting dummies
}

public class UnitAI : MonoBehaviour
{
    [Header("Unit Info")]
    public string unitName;
    public int starLevel = 1;
    public Team team = Team.Player;

    [Header("Stats")]
    public float maxHealth = 100;
    public float currentHealth;
    public float attackDamage = 10;
    public float attackSpeed = 1.0f; // attacks per second
    public float armor = 0;
    public float attackRange = 1.5f;
    public float mana = 0f;
    public float maxMana = 50f;
    [HideInInspector] public float currentMana = 0f;

    [Header("AI Settings")]
    public bool canMove = true;
    public bool canAttack = true;
    public bool isAlive = true;

    [Header("UI")]
    public GameObject unitUIPrefab;
    private UnitUI ui;

    private float attackCooldown = 0f;
    [HideInInspector] public Animator animator;
    public Transform currentTarget;

    [Header("Team Settings")]
    public int teamID = 0;   // 0 = Player team, 1 = Enemy team (expand later)

    public enum UnitState
    {
        Bench,
        BoardIdle,
        Combat
    }

    public UnitState currentState = UnitState.Bench;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        currentMana = 0f;

        if (unitUIPrefab != null)
        {
            GameObject uiObj = Instantiate(unitUIPrefab, transform);
            ui = uiObj.GetComponent<UnitUI>();
            ui.Init(transform, maxHealth, maxMana);
        }
    }

    private void Update()
    {
        if (!isAlive) return;

        if (currentState != UnitState.Combat) return; // ✅ do nothing until combat

        if (currentTarget != null && currentState == UnitState.Combat)
        {
            FaceTarget(currentTarget.position);
        }

        attackCooldown -= Time.deltaTime;

        if (currentTarget == null || !currentTarget.GetComponent<UnitAI>().isAlive)
        {
            currentTarget = FindNearestEnemy();
        }

        if (currentTarget != null && canAttack)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.position);
            if (dist <= attackRange)
            {
                if (attackCooldown <= 0f)
                {
                    Attack(currentTarget);
                    attackCooldown = 1f / attackSpeed;
                }
            }
            else if (canMove)
            {
                MoveTowards(currentTarget.position);
            }
        }
    }

    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0f; // keep upright

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
    }

    private void Attack(Transform target)
    {
        FaceTarget(target.position);

        if (animator) animator.SetTrigger("AttackTrigger");

        UnitAI enemy = target.GetComponent<UnitAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(attackDamage);
            GainMana(10);
        }
    }

    public void TakeDamage(float damage)
    {
        float reducedDamage = damage * (1f - (armor * 0.005f));
        currentHealth -= reducedDamage;
        GainMana(1);

        Debug.Log($"{unitName} took {reducedDamage} damage. HP: {currentHealth}/{maxHealth}");
        if (ui != null)
            ui.UpdateHealth(currentHealth);   // ✅ refresh bar

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isAlive = false;
        if (animator) animator.SetTrigger("DieTrigger");
        Debug.Log($"{unitName} has died!");
        GetComponent<Collider>().enabled = false;

        if (ui != null) ui.gameObject.SetActive(false); // hide bars

        this.enabled = false;
    }


    private void GainMana(float amount)
    {
        currentMana += amount;
        if (ui != null)
            ui.UpdateMana(currentMana);   // ✅ refresh bar

        if (currentMana >= maxMana)
        {
            CastAbility();
            currentMana = 0;
            if (ui != null)
                ui.UpdateMana(currentMana);   // reset bar
        }
    }


    private void CastAbility()
    {
        if (currentTarget != null)
            FaceTarget(currentTarget.transform.position);

        if (animator) animator.SetTrigger("AbilityTrigger");

        // 🔑 Run any ability component on this unit
        foreach (var ability in GetComponents<MonoBehaviour>())
        {
            if (ability is IUnitAbility unitAbility)
            {
                unitAbility.Cast();
                return;
            }
        }
    }

    public interface IUnitAbility
    {
        void Cast();
    }


    private Transform FindNearestEnemy()
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        Transform nearest = null;
        float minDist = Mathf.Infinity;

        foreach (var unit in allUnits)
        {
            if (unit == this || !unit.isAlive) continue;
            if (unit.team == this.team) continue; // ✅ don’t target allies

            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = unit.transform;
            }
        }
        return nearest;
    }
    public UnitAI GetCurrentTarget()
    {
        return currentTarget != null ? currentTarget.GetComponent<UnitAI>() : null;
    }

    private void MoveTowards(Vector3 position)
    {
        transform.position = Vector3.MoveTowards(transform.position, position, Time.deltaTime * 2f);
    }
}
