import './App.css'

import { useEffect, useMemo, useState } from 'react'

type TaskStatus = '待开始' | '进行中' | '待验收' | '已完成'
type ProjectStatus = '规划中' | '执行中' | '风险中' | '已交付'

type Member = {
  id: string
  name: string
  role: string
  department: string
  workload: number
}

type Task = {
  id: string
  title: string
  ownerId: string
  dueDate: string
  priority: '高' | '中' | '低'
  status: TaskStatus
  progress: number
}

type Project = {
  id: string
  name: string
  client: string
  managerId: string
  status: ProjectStatus
  progress: number
  budget: number
  deadline: string
  summary: string
  tasks: Task[]
}

type DashboardData = {
  members: Member[]
  projects: Project[]
}

const storageKey = 'company-project-management-v1'

const initialData: DashboardData = {
  members: [
    { id: 'm1', name: '李晨', role: '项目总监', department: 'PMO', workload: 82 },
    { id: 'm2', name: '王敏', role: '产品经理', department: '产品中心', workload: 74 },
    { id: 'm3', name: '周洋', role: '前端工程师', department: '技术研发', workload: 91 },
    { id: 'm4', name: '陈璐', role: '测试负责人', department: '质量保障', workload: 65 },
  ],
  projects: [
    {
      id: 'p1',
      name: '华东供应链协同平台',
      client: '华东商贸集团',
      managerId: 'm1',
      status: '执行中',
      progress: 68,
      budget: 480000,
      deadline: '2026-07-15',
      summary: '围绕采购、仓储、配送做一体化流程数字化，当前重点推进移动审批和报表联动。',
      tasks: [
        { id: 't1', title: '完成采购看板交互稿', ownerId: 'm2', dueDate: '2026-06-05', priority: '高', status: '进行中', progress: 75 },
        { id: 't2', title: '开发供应商对账页面', ownerId: 'm3', dueDate: '2026-06-08', priority: '高', status: '进行中', progress: 62 },
        { id: 't3', title: '回归测试与缺陷清单', ownerId: 'm4', dueDate: '2026-06-10', priority: '中', status: '待验收', progress: 88 },
      ],
    },
    {
      id: 'p2',
      name: '品牌营销数据中台',
      client: '云帆新消费',
      managerId: 'm2',
      status: '风险中',
      progress: 43,
      budget: 320000,
      deadline: '2026-08-01',
      summary: '统一广告投放、线索、转化报表，目前存在第三方接口延迟带来的上线风险。',
      tasks: [
        { id: 't4', title: '整理渠道字段映射规则', ownerId: 'm2', dueDate: '2026-06-06', priority: '中', status: '待开始', progress: 15 },
        { id: 't5', title: '搭建数据总览首页', ownerId: 'm3', dueDate: '2026-06-12', priority: '高', status: '进行中', progress: 40 },
      ],
    },
  ],
}

function currency(value: number) {
  return new Intl.NumberFormat('zh-CN', {
    style: 'currency',
    currency: 'CNY',
    maximumFractionDigits: 0,
  }).format(value)
}

function App() {
  const [data, setData] = useState<DashboardData>(() => {
    const raw = localStorage.getItem(storageKey)
    return raw ? JSON.parse(raw) : initialData
  })
  const [selectedProjectId, setSelectedProjectId] = useState(() => {
    const raw = localStorage.getItem(storageKey)
    const savedData: DashboardData | null = raw ? JSON.parse(raw) : null
    return savedData?.projects[0]?.id ?? initialData.projects[0].id
  })
  const [newProjectName, setNewProjectName] = useState('')
  const [newClientName, setNewClientName] = useState('')
  const [newTaskTitle, setNewTaskTitle] = useState('')

  useEffect(() => {
    localStorage.setItem(storageKey, JSON.stringify(data))
  }, [data])

  const selectedProject =
    data.projects.find((project) => project.id === selectedProjectId) ?? data.projects[0]

  const stats = useMemo(() => {
    const allTasks = data.projects.flatMap((project) => project.tasks)
    const completedTasks = allTasks.filter((task) => task.status === '已完成').length
    const inProgressProjects = data.projects.filter((project) => project.status === '执行中').length
    const riskProjects = data.projects.filter((project) => project.status === '风险中').length
    const totalBudget = data.projects.reduce((sum, project) => sum + project.budget, 0)
    return {
      projectCount: data.projects.length,
      taskCount: allTasks.length,
      completedTasks,
      inProgressProjects,
      riskProjects,
      totalBudget,
    }
  }, [data])

  function memberName(memberId: string) {
    return data.members.find((member) => member.id === memberId)?.name ?? '未分配'
  }

  function addProject() {
    if (!newProjectName.trim() || !newClientName.trim()) return
    const project: Project = {
      id: crypto.randomUUID(),
      name: newProjectName.trim(),
      client: newClientName.trim(),
      managerId: data.members[0]?.id ?? '',
      status: '规划中',
      progress: 0,
      budget: 100000,
      deadline: new Date().toISOString().slice(0, 10),
      summary: '新建项目，等待补充背景、里程碑与交付范围。',
      tasks: [],
    }
    setData((current) => ({ ...current, projects: [project, ...current.projects] }))
    setSelectedProjectId(project.id)
    setNewProjectName('')
    setNewClientName('')
  }

  function addTask() {
    if (!selectedProject || !newTaskTitle.trim()) return
    const task: Task = {
      id: crypto.randomUUID(),
      title: newTaskTitle.trim(),
      ownerId: data.members[0]?.id ?? '',
      dueDate: new Date().toISOString().slice(0, 10),
      priority: '中',
      status: '待开始',
      progress: 0,
    }
    setData((current) => ({
      ...current,
      projects: current.projects.map((project) =>
        project.id === selectedProject.id
          ? { ...project, tasks: [task, ...project.tasks] }
          : project,
      ),
    }))
    setNewTaskTitle('')
  }

  function cycleTaskStatus(taskId: string) {
    if (!selectedProject) return
    const order: TaskStatus[] = ['待开始', '进行中', '待验收', '已完成']
    setData((current) => ({
      ...current,
      projects: current.projects.map((project) => {
        if (project.id !== selectedProject.id) return project
        const updatedTasks = project.tasks.map((task) => {
          if (task.id !== taskId) return task
          const nextStatus = order[(order.indexOf(task.status) + 1) % order.length]
          const progressMap: Record<TaskStatus, number> = {
            待开始: 0,
            进行中: 55,
            待验收: 90,
            已完成: 100,
          }
          return { ...task, status: nextStatus, progress: progressMap[nextStatus] }
        })
        const avgProgress = updatedTasks.length
          ? Math.round(updatedTasks.reduce((sum, task) => sum + task.progress, 0) / updatedTasks.length)
          : 0
        return { ...project, tasks: updatedTasks, progress: avgProgress }
      }),
    }))
  }

  return (
    <div className="shell">
      <aside className="sidebar">
        <div>
          <p className="eyebrow">DesignFlow 驱动</p>
          <h1>公司项目管理系统</h1>
          <p className="muted">Windows 桌面版原型，覆盖项目总览、任务推进、成员负载和进度分析。</p>
        </div>

        <div className="card formCard">
          <h3>新建项目</h3>
          <input value={newProjectName} onChange={(e) => setNewProjectName(e.target.value)} placeholder="项目名称" />
          <input value={newClientName} onChange={(e) => setNewClientName(e.target.value)} placeholder="客户 / 业务部门" />
          <button onClick={addProject}>创建项目</button>
        </div>

        <div className="card">
          <h3>项目列表</h3>
          <div className="projectList">
            {data.projects.map((project) => (
              <button
                key={project.id}
                className={`projectItem ${project.id === selectedProject?.id ? 'active' : ''}`}
                onClick={() => setSelectedProjectId(project.id)}
              >
                <strong>{project.name}</strong>
                <span>{project.client}</span>
                <em>{project.status}</em>
              </button>
            ))}
          </div>
        </div>
      </aside>

      <main className="main">
        <section className="hero">
          <div>
            <p className="eyebrow">管理驾驶舱</p>
            <h2>让项目状态、人员投入和交付风险一屏看清</h2>
            <p className="muted">
              适合公司内部项目、客户交付项目、信息化建设项目做统一管理。数据保存在本机，启动后即可操作。
            </p>
          </div>
          <div className="heroPanel">
            <span>总预算池</span>
            <strong>{currency(stats.totalBudget)}</strong>
            <small>在管项目 {stats.projectCount} 个，风险项目 {stats.riskProjects} 个</small>
          </div>
        </section>

        <section className="statGrid">
          <article className="statCard"><span>项目总数</span><strong>{stats.projectCount}</strong></article>
          <article className="statCard"><span>任务总数</span><strong>{stats.taskCount}</strong></article>
          <article className="statCard"><span>已完成任务</span><strong>{stats.completedTasks}</strong></article>
          <article className="statCard"><span>执行中项目</span><strong>{stats.inProgressProjects}</strong></article>
        </section>

        {selectedProject && (
          <section className="contentGrid">
            <article className="card projectOverview">
              <div className="sectionHead">
                <div>
                  <p className="eyebrow">当前项目</p>
                  <h3>{selectedProject.name}</h3>
                </div>
                <span className={`badge status-${selectedProject.status}`}>{selectedProject.status}</span>
              </div>
              <p>{selectedProject.summary}</p>
              <div className="metaGrid">
                <div><span>客户单位</span><strong>{selectedProject.client}</strong></div>
                <div><span>项目经理</span><strong>{memberName(selectedProject.managerId)}</strong></div>
                <div><span>预算</span><strong>{currency(selectedProject.budget)}</strong></div>
                <div><span>截止日期</span><strong>{selectedProject.deadline}</strong></div>
              </div>
              <div>
                <div className="progressRow">
                  <span>总体进度</span>
                  <strong>{selectedProject.progress}%</strong>
                </div>
                <div className="progressBar">
                  <div style={{ width: `${selectedProject.progress}%` }} />
                </div>
              </div>
            </article>

            <article className="card">
              <div className="sectionHead">
                <h3>成员负载</h3>
                <span className="muted">按当前排期估算</span>
              </div>
              <div className="memberList">
                {data.members.map((member) => (
                  <div key={member.id} className="memberItem">
                    <div>
                      <strong>{member.name}</strong>
                      <span>{member.department} / {member.role}</span>
                    </div>
                    <div className="loadWrap">
                      <em>{member.workload}%</em>
                      <div className="miniBar"><div style={{ width: `${member.workload}%` }} /></div>
                    </div>
                  </div>
                ))}
              </div>
            </article>

            <article className="card fullWidth">
              <div className="sectionHead">
                <h3>任务看板</h3>
                <div className="inlineForm">
                  <input value={newTaskTitle} onChange={(e) => setNewTaskTitle(e.target.value)} placeholder="新增任务标题" />
                  <button onClick={addTask}>添加任务</button>
                </div>
              </div>
              <div className="kanban">
                {(['待开始', '进行中', '待验收', '已完成'] as TaskStatus[]).map((status) => (
                  <div key={status} className="kanbanColumn">
                    <h4>{status}</h4>
                    {selectedProject.tasks.filter((task) => task.status === status).map((task) => (
                      <button key={task.id} className="taskCard" onClick={() => cycleTaskStatus(task.id)}>
                        <strong>{task.title}</strong>
                        <span>负责人：{memberName(task.ownerId)}</span>
                        <span>截止：{task.dueDate}</span>
                        <div className="taskFoot">
                          <em className={`priority priority-${task.priority}`}>{task.priority}优先</em>
                          <small>{task.progress}%</small>
                        </div>
                      </button>
                    ))}
                  </div>
                ))}
              </div>
            </article>
          </section>
        )}
      </main>
    </div>
  )
}

export default App
