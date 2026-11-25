<script setup lang="ts">
import {ref, onMounted} from 'vue'
import Chart from 'chart.js/auto'

const props = defineProps<{
    values: number[]
    aspectRatio?: number
    legendPosition?: 'right' | 'bottom'
}>()

const canvas = ref<HTMLCanvasElement | null>(null)

onMounted(() =>
    new Chart(canvas.value!, {
        type: 'doughnut',
        data: {
            labels: ['Active', 'Expiring soon', 'Expired', 'Cancelled'],
            datasets: [{
                data: props.values,
                backgroundColor: ['#2f61e2', '#6c91ec', '#1d3f99', '#c6d6f9']
            }],
        },
        options: {
            aspectRatio: props.aspectRatio ?? 2,
            plugins: { legend: { position: props.legendPosition ?? 'right'}},
        },
    
    })
)
</script>
<template>
    <canvas ref="canvas"></canvas>
</template>