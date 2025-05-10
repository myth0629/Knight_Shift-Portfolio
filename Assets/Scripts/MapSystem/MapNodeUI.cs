using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace MapSystem
{
    public class MapNodeUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Node References")]
        [SerializeField] private Image nodeImage;
        [SerializeField] private Image nodeOutline;
        [SerializeField] private TextMeshProUGUI nodeLabel;
        [SerializeField] private RectTransform connectionContainer;
        [SerializeField] private GameObject connectionPrefab;
        
        [Header("Node Type Sprites")]
        [SerializeField] private Sprite battleSprite;
        [SerializeField] private Sprite eventSprite;
        [SerializeField] private Sprite shopSprite;
        [SerializeField] private Sprite campSprite;
        [SerializeField] private Sprite eliteSprite;
        [SerializeField] private Sprite bossSprite;
        [SerializeField] private Sprite startSprite;
        
        [Header("Node Colors")]
        [SerializeField] private Color battleColor = Color.red;
        [SerializeField] private Color eventColor = Color.blue;
        [SerializeField] private Color shopColor = Color.green;
        [SerializeField] private Color campColor = Color.yellow;
        [SerializeField] private Color eliteColor = new Color(0.8f, 0.2f, 0.8f); // Purple
        [SerializeField] private Color bossColor = new Color(0.8f, 0.1f, 0.1f); // Dark Red
        [SerializeField] private Color startColor = Color.white;
        [SerializeField] private Color accessibleColor = Color.white;
        [SerializeField] private Color inaccessibleColor = Color.gray;
        [SerializeField] private Color highlightedColor = Color.yellow;
        
        [Header("Animation")]
        [SerializeField] private float pulseSpeed = 1f;
        [SerializeField] private float pulseSize = 0.1f;
        
        private MapNode nodeData;
        private List<RectTransform> connections = new List<RectTransform>();
        private bool isAccessible = false;
        private bool isHighlighted = false;
        
        public void Initialize(MapNode node, bool accessible)
        {
            nodeData = node;
            isAccessible = accessible;
            
            // Set position based on node data
            RectTransform rectTransform = GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(node.position.x * 100f, node.position.y * 100f);
            
            // Set appropriate sprite and color based on node type
            UpdateNodeVisuals();
            
            // Set label if needed
            if (nodeLabel != null)
            {
                nodeLabel.text = GetNodeLabel();
            }
        }
        
        public void UpdateAccessibility(bool accessible)
        {
            isAccessible = accessible;
            UpdateNodeVisuals();
        }
        
        private void UpdateNodeVisuals()
        {
            if (nodeImage != null)
            {
                nodeImage.sprite = GetSpriteForNodeType(nodeData.nodeType);
                
                // Set color based on node type and accessibility
                Color baseColor = GetColorForNodeType(nodeData.nodeType);
                nodeImage.color = isAccessible ? baseColor : Color.Lerp(baseColor, inaccessibleColor, 0.7f);
            }
            
            if (nodeOutline != null)
            {
                nodeOutline.color = isAccessible ? accessibleColor : inaccessibleColor;
            }
        }
        
        private Sprite GetSpriteForNodeType(NodeType type)
        {
            switch (type)
            {
                case NodeType.Battle: return battleSprite;
                case NodeType.Event: return eventSprite;
                case NodeType.Shop: return shopSprite;
                case NodeType.Camp: return campSprite;
                case NodeType.Elite: return eliteSprite;
                case NodeType.Boss: return bossSprite;
                case NodeType.Start: return startSprite;
                default: return battleSprite;
            }
        }
        
        private Color GetColorForNodeType(NodeType type)
        {
            switch (type)
            {
                case NodeType.Battle: return battleColor;
                case NodeType.Event: return eventColor;
                case NodeType.Shop: return shopColor;
                case NodeType.Camp: return campColor;
                case NodeType.Elite: return eliteColor;
                case NodeType.Boss: return bossColor;
                case NodeType.Start: return startColor;
                default: return battleColor;
            }
        }
        
        private string GetNodeLabel()
        {
            switch (nodeData.nodeType)
            {
                case NodeType.Battle: return "전투";
                case NodeType.Event: return "이벤트";
                case NodeType.Shop: return "상점";
                case NodeType.Camp: return "캠프";
                case NodeType.Elite: return "엘리트";
                case NodeType.Boss: return "보스";
                case NodeType.Start: return "시작";
                default: return "";
            }
        }
        
        public void CreateConnectionTo(MapNodeUI targetNode)
        {
            if (connectionContainer == null || connectionPrefab == null) return;
            
            // Create a new connection line
            GameObject connectionObj = Instantiate(connectionPrefab, connectionContainer);
            RectTransform connectionRect = connectionObj.GetComponent<RectTransform>();
            connections.Add(connectionRect);
            
            // 연결선을 계층 구조에서 가장 처음에 배치하여 다른 UI 요소보다 뒤에 표시되도록 함
            connectionObj.transform.SetAsFirstSibling();
            
            // Get positions in local space
            Vector2 startPos = Vector2.zero; // Local space, so this node is at origin
            Vector2 endPos = targetNode.GetComponent<RectTransform>().anchoredPosition - GetComponent<RectTransform>().anchoredPosition;
            
            // Calculate distance and angle
            float distance = Vector2.Distance(startPos, endPos);
            float angle = Mathf.Atan2(endPos.y - startPos.y, endPos.x - startPos.x) * Mathf.Rad2Deg;
            
            // Set line position, size and rotation
            connectionRect.sizeDelta = new Vector2(distance, connectionRect.sizeDelta.y);
            connectionRect.anchoredPosition = startPos + (endPos - startPos) / 2f;
            connectionRect.localRotation = Quaternion.Euler(0, 0, angle);
            
            // Set color based on accessibility
            Image connectionImage = connectionRect.GetComponent<Image>();
            if (connectionImage != null)
            {
                connectionImage.color = isAccessible ? accessibleColor : inaccessibleColor;
            }
        }
        
        public void ClearConnections()
        {
            foreach (var connection in connections)
            {
                if (connection != null)
                {
                    Destroy(connection.gameObject);
                }
            }
            
            connections.Clear();
        }
        
        private void Update()
        {
            if (isAccessible)
            {
                // Pulse animation for accessible nodes
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseSize;
                transform.localScale = Vector3.one * pulse;
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (isAccessible)
            {
                // Select this node
                MapController.Instance.SelectNode(nodeData.id);
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isAccessible)
            {
                isHighlighted = true;
                if (nodeOutline != null)
                {
                    nodeOutline.color = highlightedColor;
                }
                
                // Show tooltip or info about the node
                MapUIManager.Instance?.ShowNodeTooltip(nodeData, transform.position);
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            isHighlighted = false;
            if (nodeOutline != null)
            {
                nodeOutline.color = isAccessible ? accessibleColor : inaccessibleColor;
            }
            
            // Hide tooltip
            MapUIManager.Instance?.HideNodeTooltip();
        }
        
        public MapNode GetNodeData()
        {
            return nodeData;
        }
    }
}
