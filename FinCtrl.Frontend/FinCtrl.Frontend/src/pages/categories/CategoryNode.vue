<template>
    <div class="category-node" :style="{ paddingLeft: depth * 20 + 'px' }">
        <div class="d-flex align-items-center py-1 node-row">
            <button
                class="btn btn-link btn-sm p-0 me-1 expand-btn"
                @click="expanded = !expanded"
                v-if="node.children && node.children.length > 0"
            >
                {{ expanded ? '▼' : '▶' }}
            </button>
            <span v-else class="expand-placeholder me-1"></span>

            <span class="node-color-dot me-2" :style="{ background: node.color || '#808080' }"></span>

            <span class="node-name me-2">{{ node.categoryName }}</span>

            <span v-if="node.isOnlyIncome" class="badge bg-info text-white me-2" style="font-size: 0.7rem;">Только доход</span>

            <small class="text-muted me-3">#{{ node.orderIndex }}</small>

            <div class="btn-group btn-group-sm ms-auto">
                <button
                    class="btn btn-outline-secondary"
                    title="Добавить подкатегорию"
                    @click="$emit('add-child', node)"
                >➕</button>
                <button
                    class="btn btn-outline-secondary"
                    title="Редактировать"
                    @click="$emit('edit', node)"
                >✏️</button>
                <button
                    class="btn btn-outline-danger"
                    title="Удалить"
                    @click="$emit('delete', node)"
                >🗑️</button>
            </div>
        </div>

        <div v-if="expanded && node.children && node.children.length > 0">
            <CategoryNode
                v-for="child in node.children"
                :key="child.categoryId"
                :node="child"
                :depth="depth + 1"
                @edit="$emit('edit', $event)"
                @delete="$emit('delete', $event)"
                @add-child="$emit('add-child', $event)"
            />
        </div>
    </div>
</template>

<script setup>
import { ref } from 'vue'

const props = defineProps({
    node: { type: Object, required: true },
    depth: { type: Number, default: 0 }
})

defineEmits(['edit', 'delete', 'add-child'])

const expanded = ref(true)
</script>

<style scoped>
.node-row {
    border-radius: 4px;
    padding-right: 8px;
    transition: background-color 0.1s;
}
.node-row:hover {
    background-color: #f8f9fa;
}
.node-name {
    font-size: 0.95rem;
}
.expand-btn {
    color: #6c757d;
    font-size: 0.7rem;
    width: 18px;
    text-align: center;
    text-decoration: none;
}
.expand-placeholder {
    display: inline-block;
    width: 18px;
}
.node-color-dot {
    display: inline-block;
    width: 10px;
    height: 10px;
    border-radius: 50%;
    flex-shrink: 0;
}
</style>
